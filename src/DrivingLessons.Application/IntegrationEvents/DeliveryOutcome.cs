namespace DrivingLessons.Application.IntegrationEvents;

public record DeliveryOutcome
{
    public static DeliveryOutcome Done { get; } = new((DateTimeOffset?)null);

    public DateTimeOffset? HoldUntilUtc { get; }

    public bool IsHeld => HoldUntilUtc is not null;

    private DeliveryOutcome(DateTimeOffset? holdUntilUtc)
    {
        HoldUntilUtc = holdUntilUtc;
    }

    public static DeliveryOutcome HoldUntil(DateTimeOffset untilUtc)
    {
        return new DeliveryOutcome(untilUtc);
    }
}
