namespace DrivingLessons.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> UncommittedEvents { get; }

    void CommitEvents();
}
