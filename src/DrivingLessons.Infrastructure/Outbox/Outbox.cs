using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;

namespace DrivingLessons.Infrastructure.Outbox;

public class Outbox : IOutbox
{
    private readonly DrivingLessonsDbContext context;
    private readonly IntegrationEventCatalog catalog;
    private readonly TimeProvider timeProvider;

    public Outbox(DrivingLessonsDbContext context, IntegrationEventCatalog catalog, TimeProvider timeProvider)
    {
        this.context = context;
        this.catalog = catalog;
        this.timeProvider = timeProvider;
    }

    public void Add(IIntegrationEvent integrationEvent)
    {
        var eventType = integrationEvent.GetType();
        var eventName = catalog.NameOf(eventType);
        var consumers = catalog.ConsumersOf(eventName);
        var payload = OutboxSerializer.Serialize(integrationEvent);
        var nowUtc = timeProvider.GetUtcNow();

        var message = OutboxMessage.Create(eventName, payload, nowUtc);
        context.OutboxMessages.Add(message);

        foreach (var consumer in consumers)
        {
            var delivery = OutboxDelivery.Create(message.Id, consumer, nowUtc);
            context.OutboxDeliveries.Add(delivery);
        }
    }
}
