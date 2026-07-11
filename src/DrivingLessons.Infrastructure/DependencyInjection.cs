using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DrivingLessonsDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DrivingLessonsDbContext>());

        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAdminAccountGateway, AdminAccountGateway>();
        services.AddSingleton<IPasswordVerifier, PasswordVerifier>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
