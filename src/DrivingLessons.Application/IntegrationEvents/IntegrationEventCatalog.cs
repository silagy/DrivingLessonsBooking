using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application.IntegrationEvents;

public class IntegrationEventCatalog
{
    private readonly Dictionary<Type, string> namesByType = [];
    private readonly Dictionary<string, Type> typesByName = [];
    private readonly Dictionary<string, List<IntegrationEventConsumerRegistration>> consumersByEvent = [];

    public void AddEvent<TEvent>(string name)
        where TEvent : IIntegrationEvent
    {
        var eventType = typeof(TEvent);

        if (typesByName.ContainsKey(name))
        {
            throw new IntegrationEventCatalogException($"Integration event name [{name}] is already registered.");
        }

        if (namesByType.ContainsKey(eventType))
        {
            throw new IntegrationEventCatalogException($"Integration event [{eventType.Name}] is already registered.");
        }

        namesByType.Add(eventType, name);
        typesByName.Add(name, eventType);
        consumersByEvent.Add(name, []);
    }

    public void AddConsumer<TEvent, TConsumer>()
        where TEvent : IIntegrationEvent
        where TConsumer : IIntegrationEventConsumer<TEvent>
    {
        var eventName = NameOf(typeof(TEvent));
        var consumerName = typeof(TConsumer).Name;
        var consumers = consumersByEvent[eventName];

        if (consumers.Any(x => x.Name == consumerName))
        {
            throw new IntegrationEventCatalogException($"Consumer [{consumerName}] of [{eventName}] is already registered.");
        }

        var registration = new IntegrationEventConsumerRegistration(consumerName, ConsumeAsync);
        consumers.Add(registration);

        static Task<DeliveryOutcome> ConsumeAsync(IServiceProvider provider, IIntegrationEvent integrationEvent)
        {
            var consumer = provider.GetRequiredService<TConsumer>();

            return consumer.ConsumeAsync((TEvent)integrationEvent);
        }
    }

    public string NameOf(Type eventType)
    {
        if (!namesByType.TryGetValue(eventType, out var name))
        {
            throw new IntegrationEventCatalogException($"Integration event [{eventType.Name}] is not registered.");
        }

        return name;
    }

    public Type TypeOf(string eventName)
    {
        if (!typesByName.TryGetValue(eventName, out var eventType))
        {
            throw new IntegrationEventCatalogException($"Integration event name [{eventName}] is not registered.");
        }

        return eventType;
    }

    public IReadOnlyList<string> ConsumersOf(string eventName)
    {
        var consumers = RegistrationsOf(eventName);

        return consumers.Select(x => x.Name).ToList();
    }

    public IntegrationEventConsumerRegistration ConsumerOf(string eventName, string consumerName)
    {
        var consumers = RegistrationsOf(eventName);
        var registration = consumers.FirstOrDefault(x => x.Name == consumerName);

        return registration
               ?? throw new IntegrationEventCatalogException($"Consumer [{consumerName}] of [{eventName}] is not registered.");
    }

    private List<IntegrationEventConsumerRegistration> RegistrationsOf(string eventName)
    {
        if (!consumersByEvent.TryGetValue(eventName, out var consumers))
        {
            throw new IntegrationEventCatalogException($"Integration event name [{eventName}] is not registered.");
        }

        return consumers;
    }
}
