namespace DrivingLessons.Application.IntegrationEvents;

public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task<DeliveryOutcome> ConsumeAsync(TEvent integrationEvent);
}
