namespace DrivingLessons.Application.IntegrationEvents;

public record IntegrationEventConsumerRegistration(string Name, Func<IServiceProvider, IIntegrationEvent, Task<DeliveryOutcome>> InvokeAsync);
