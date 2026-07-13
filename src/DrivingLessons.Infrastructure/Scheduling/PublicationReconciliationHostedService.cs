using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Commands.ClosePublication;
using DrivingLessons.Application.Commands.OpenPublication;
using DrivingLessons.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DrivingLessons.Infrastructure.Scheduling;

public class PublicationReconciliationHostedService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly TimeProvider timeProvider;

    public PublicationReconciliationHostedService(IServiceProvider serviceProvider, TimeProvider timeProvider)
    {
        this.serviceProvider = serviceProvider;
        this.timeProvider = timeProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var repository = provider.GetRequiredService<IPublicationRepository>();
        var openInteractor = provider.GetRequiredService<OpenPublicationInteractor>();
        var closeInteractor = provider.GetRequiredService<ClosePublicationInteractor>();
        var scheduler = provider.GetRequiredService<IPublicationScheduler>();

        var nowUtc = timeProvider.GetUtcNow();

        var dueToOpen = await repository.GetPublishedDueToOpenAsync(nowUtc);
        foreach (var publication in dueToOpen)
        {
            await openInteractor.ExecuteAsync(publication.Id.Value);
        }

        var dueToClose = await repository.GetOpenDueToCloseAsync(nowUtc);
        foreach (var publication in dueToClose)
        {
            await closeInteractor.ExecuteAsync(publication.Id.Value);
        }

        await RegisterFutureJobsAsync(repository, scheduler);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task RegisterFutureJobsAsync(IPublicationRepository repository, IPublicationScheduler scheduler)
    {
        var pendingOpen = await repository.GetPublishedDueToOpenAsync(DateTimeOffset.MaxValue);
        foreach (var publication in pendingOpen)
        {
            var window = publication.Window!;
            await scheduler.SchedulePublicationJobsAsync(publication.Id.Value, window.StartUtc, window.EndUtc);
        }

        var pendingClose = await repository.GetOpenDueToCloseAsync(DateTimeOffset.MaxValue);
        foreach (var publication in pendingClose)
        {
            var window = publication.Window!;
            await scheduler.RescheduleCloseAsync(publication.Id.Value, window.EndUtc);
        }
    }
}
