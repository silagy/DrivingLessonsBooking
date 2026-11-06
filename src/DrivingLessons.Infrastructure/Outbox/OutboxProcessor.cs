using System.Text.Json;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxProcessor
{
    private const int PurgeBatchSize = 500;

    private readonly DrivingLessonsDbContext context;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IntegrationEventCatalog catalog;
    private readonly OutboxOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OutboxProcessor> logger;

    public OutboxProcessor(
        DrivingLessonsDbContext context,
        IServiceScopeFactory scopeFactory,
        IntegrationEventCatalog catalog,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxProcessor> logger)
    {
        this.context = context;
        this.scopeFactory = scopeFactory;
        this.catalog = catalog;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        var deliveryIds = await LeaseDueAsync();

        foreach (var deliveryId in deliveryIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await DeliverAsync(deliveryId);
        }
    }

    public async Task PurgeAsync()
    {
        var nowUtc = timeProvider.GetUtcNow();
        var cutoffUtc = nowUtc.AddDays(-options.RetentionDays);
        var succeeded = nameof(OutboxDeliveryState.Succeeded);

        await context.Database.ExecuteSqlAsync(
            $"""
             DELETE FROM outbox_deliveries
             WHERE id IN (
                 SELECT id FROM outbox_deliveries
                 WHERE state = {succeeded} AND completed_at_utc < {cutoffUtc}
                 LIMIT {PurgeBatchSize})
             """);

        await context.Database.ExecuteSqlAsync(
            $"""
             DELETE FROM outbox_messages AS m
             WHERE m.occurred_at_utc < {cutoffUtc}
               AND NOT EXISTS (SELECT 1 FROM outbox_deliveries AS d WHERE d.message_id = m.id)
             """);
    }

    private async Task<List<Guid>> LeaseDueAsync()
    {
        var nowUtc = timeProvider.GetUtcNow();
        var leasedUntilUtc = nowUtc.AddSeconds(options.LeaseSeconds);
        var pending = nameof(OutboxDeliveryState.Pending);
        var batchSize = options.BatchSize;

        return await context.Database
                            .SqlQuery<Guid>(
                                $"""
                                 UPDATE outbox_deliveries
                                 SET locked_until_utc = {leasedUntilUtc}
                                 WHERE id IN (
                                     SELECT id FROM outbox_deliveries
                                     WHERE state = {pending}
                                       AND next_attempt_at_utc <= {nowUtc}
                                       AND (locked_until_utc IS NULL OR locked_until_utc < {nowUtc})
                                     ORDER BY next_attempt_at_utc
                                     LIMIT {batchSize}
                                     FOR UPDATE SKIP LOCKED)
                                 RETURNING id AS "Value"
                                 """)
                            .ToListAsync();
    }

    private async Task DeliverAsync(Guid deliveryId)
    {
        var delivery = await context.OutboxDeliveries.SingleAsync(x => x.Id == deliveryId);
        var message = await context.OutboxMessages.SingleAsync(x => x.Id == delivery.MessageId);

        IntegrationEventConsumerRegistration registration;
        IIntegrationEvent integrationEvent;

        try
        {
            registration = catalog.ConsumerOf(message.EventName, delivery.Consumer);
            var eventType = catalog.TypeOf(message.EventName);
            integrationEvent = OutboxSerializer.Deserialize(message.Payload, eventType);
        }
        catch (Exception exception) when (exception is IntegrationEventCatalogException or JsonException)
        {
            var deadAtUtc = timeProvider.GetUtcNow();
            delivery.MarkDead(exception, deadAtUtc);

            LogFailure(exception, message, delivery);

            await context.SaveChangesAsync();
            return;
        }

        await RunConsumerAsync(registration, integrationEvent, message, delivery);

        await context.SaveChangesAsync();
    }

    private async Task RunConsumerAsync(
        IntegrationEventConsumerRegistration registration,
        IIntegrationEvent integrationEvent,
        OutboxMessage message,
        OutboxDelivery delivery)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var outcome = await registration.InvokeAsync(scope.ServiceProvider, integrationEvent);

            Apply(outcome, delivery);
        }
        catch (Exception exception)
        {
            var failedAtUtc = timeProvider.GetUtcNow();
            delivery.Fail(exception, failedAtUtc, options.MaxAttempts);

            LogFailure(exception, message, delivery);
        }
    }

    private void Apply(DeliveryOutcome outcome, OutboxDelivery delivery)
    {
        if (outcome.HoldUntilUtc is { } holdUntilUtc)
        {
            delivery.Hold(holdUntilUtc);
            return;
        }

        var completedAtUtc = timeProvider.GetUtcNow();
        delivery.Succeed(completedAtUtc);
    }

    private void LogFailure(Exception exception, OutboxMessage message, OutboxDelivery delivery)
    {
        if (delivery.State == OutboxDeliveryState.Dead)
        {
            logger.LogError(
                exception,
                "Outbox delivery [{DeliveryId}] of [{EventName}] to [{Consumer}] is dead after [{Attempts}] attempts.",
                delivery.Id,
                message.EventName,
                delivery.Consumer,
                delivery.Attempts);

            return;
        }

        logger.LogWarning(
            exception,
            "Outbox delivery [{DeliveryId}] of [{EventName}] to [{Consumer}] failed on attempt [{Attempts}], next at [{NextAttemptAtUtc}].",
            delivery.Id,
            message.EventName,
            delivery.Consumer,
            delivery.Attempts,
            delivery.NextAttemptAtUtc);
    }
}
