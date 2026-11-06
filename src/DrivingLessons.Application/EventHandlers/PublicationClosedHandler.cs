using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Application.EventHandlers;

public class PublicationClosedHandler : IDomainEventHandler<PublicationClosed>
{
    private readonly IPublicationRepository repository;
    private readonly IOutbox outbox;

    public PublicationClosedHandler(IPublicationRepository repository, IOutbox outbox)
    {
        this.repository = repository;
        this.outbox = outbox;
    }

    public async Task HandleAsync(PublicationClosed domainEvent)
    {
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        var teachers = publication.TeacherVersions
                                  .Select(x => new ClosedTeacherVersion(x.TeacherId.Value, x.Version))
                                  .ToList();
        var publicationId = publication.Id.Value;

        var closed = new PublicationClosedIntegrationEvent(publicationId, teachers);
        outbox.Add(closed);
    }
}
