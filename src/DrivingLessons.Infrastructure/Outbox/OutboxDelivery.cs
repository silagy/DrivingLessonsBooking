namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxDelivery
{
    public const int LastErrorMaxLength = 1000;

    private const int MaxBackoffMinutes = 60;

    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string Consumer { get; private set; } = string.Empty;

    public OutboxDeliveryState State { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private OutboxDelivery()
    {
    }

    private OutboxDelivery(Guid id, Guid messageId, string consumer, DateTimeOffset nextAttemptAtUtc)
    {
        Id = id;
        MessageId = messageId;
        Consumer = consumer;
        State = OutboxDeliveryState.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    public static OutboxDelivery Create(Guid messageId, string consumer, DateTimeOffset nowUtc)
    {
        var id = Guid.CreateVersion7();

        return new OutboxDelivery(id, messageId, consumer, nowUtc);
    }

    public void Succeed(DateTimeOffset nowUtc)
    {
        MustBePending();

        Attempts++;
        State = OutboxDeliveryState.Succeeded;
        CompletedAtUtc = nowUtc;
        LockedUntilUtc = null;
    }

    public void Fail(Exception exception, DateTimeOffset nowUtc, int maxAttempts)
    {
        MustBePending();

        Attempts++;
        LastError = Describe(exception);
        LockedUntilUtc = null;

        if (Attempts >= maxAttempts)
        {
            State = OutboxDeliveryState.Dead;
            CompletedAtUtc = nowUtc;
            return;
        }

        var backoff = BackoffAfter(Attempts);
        NextAttemptAtUtc = nowUtc.Add(backoff);
    }

    public void Hold(DateTimeOffset untilUtc)
    {
        MustBePending();

        NextAttemptAtUtc = untilUtc;
        LockedUntilUtc = null;
    }

    public void MarkDead(Exception exception, DateTimeOffset nowUtc)
    {
        MustBePending();

        Attempts++;
        LastError = Describe(exception);
        LockedUntilUtc = null;
        State = OutboxDeliveryState.Dead;
        CompletedAtUtc = nowUtc;
    }

    private void MustBePending()
    {
        if (State != OutboxDeliveryState.Pending)
        {
            throw new OutboxDeliveryMustBePendingException(Id, State);
        }
    }

    private static TimeSpan BackoffAfter(int attempts)
    {
        var exponent = attempts - 1;
        var minutes = Math.Min(Math.Pow(2, exponent), MaxBackoffMinutes);

        return TimeSpan.FromMinutes(minutes);
    }

    private static string Describe(Exception exception)
    {
        var typeName = exception.GetType().Name;
        var description = $"{typeName}: {exception.Message}";

        return description.Length <= LastErrorMaxLength
            ? description
            : description[..LastErrorMaxLength];
    }
}
