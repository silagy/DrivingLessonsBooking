namespace DrivingLessons.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : EntityId
{
    private readonly List<IDomainEvent> uncommittedEvents = [];

    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => uncommittedEvents.AsReadOnly();

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    protected void AddEvent(IDomainEvent domainEvent)
    {
        uncommittedEvents.Add(domainEvent);
    }

    public void CommitEvents()
    {
        uncommittedEvents.Clear();
    }
}
