using System.Text.Json;
using DrivingLessons.Application.IntegrationEvents;

namespace DrivingLessons.Infrastructure.Outbox;

public static class OutboxSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IIntegrationEvent integrationEvent)
    {
        var eventType = integrationEvent.GetType();

        return JsonSerializer.Serialize(integrationEvent, eventType, Options);
    }

    public static IIntegrationEvent Deserialize(string payload, Type eventType)
    {
        var restored = JsonSerializer.Deserialize(payload, eventType, Options);

        return (IIntegrationEvent)restored!;
    }
}
