namespace DrivingLessons.Infrastructure.Outbox;

public enum OutboxDeliveryState
{
    Pending,
    Succeeded,
    Dead
}
