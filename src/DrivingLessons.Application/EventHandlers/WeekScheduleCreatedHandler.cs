using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Application.EventHandlers;

public class WeekScheduleCreatedHandler : IDomainEventHandler<WeekScheduleCreated>
{
    private readonly IPublicationRepository repository;

    public WeekScheduleCreatedHandler(IPublicationRepository repository)
    {
        this.repository = repository;
    }

    public async Task HandleAsync(WeekScheduleCreated domainEvent)
    {
        var existing = await repository.GetByWeekAsync(domainEvent.WeekStart);

        if (existing is not null)
        {
            return;
        }

        var publication = Publication.Create(domainEvent.WeekStart);

        repository.Add(publication);
    }
}
