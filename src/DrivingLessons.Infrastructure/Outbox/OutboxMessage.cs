namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    private OutboxMessage()
    {
    }

    private OutboxMessage(Guid id, string eventName, string payload, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public static OutboxMessage Create(string eventName, string payload, DateTimeOffset occurredAtUtc)
    {
        var id = Guid.CreateVersion7();

        return new OutboxMessage(id, eventName, payload, occurredAtUtc);
    }
}
