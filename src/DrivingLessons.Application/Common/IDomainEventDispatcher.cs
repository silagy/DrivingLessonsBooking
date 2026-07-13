using DrivingLessons.Domain.Common;

namespace DrivingLessons.Application.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
}
