namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxDeliveryMustBePendingException : Exception
{
    public OutboxDeliveryMustBePendingException(Guid deliveryId, OutboxDeliveryState state)
        : base($"Outbox delivery [{deliveryId}] must be pending but is [{state}].")
    {
    }
}
