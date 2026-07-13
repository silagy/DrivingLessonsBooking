using DrivingLessons.Domain.Common;

namespace DrivingLessons.Application.Common;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}
