using DrivingLessons.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginInteractor>();
        return services;
    }
}
