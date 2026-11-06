using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application.IntegrationEvents;

public static class IntegrationEventServiceCollectionExtension
{
    public static IServiceCollection AddIntegrationEventCatalog(this IServiceCollection services)
    {
        CatalogOf(services);

        return services;
    }

    public static IServiceCollection AddIntegrationEvent<TEvent>(this IServiceCollection services, string name)
        where TEvent : IIntegrationEvent
    {
        var catalog = CatalogOf(services);
        catalog.AddEvent<TEvent>(name);

        return services;
    }

    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TConsumer>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        var catalog = CatalogOf(services);
        catalog.AddConsumer<TEvent, TConsumer>();

        services.AddScoped<TConsumer>();

        return services;
    }

    private static IntegrationEventCatalog CatalogOf(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IntegrationEventCatalog));

        if (descriptor?.ImplementationInstance is IntegrationEventCatalog existing)
        {
            return existing;
        }

        var catalog = new IntegrationEventCatalog();
        services.AddSingleton(catalog);

        return catalog;
    }
}
