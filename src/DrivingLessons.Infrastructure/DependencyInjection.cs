using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.DomainEvents;
using DrivingLessons.Infrastructure.Email;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.EntityFramework.Queries;
using DrivingLessons.Infrastructure.EntityFramework.Repositories;
using DrivingLessons.Infrastructure.Excel;
using DrivingLessons.Infrastructure.Options;
using DrivingLessons.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace DrivingLessons.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

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

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAdminAccountGateway, AdminAccountGateway>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ITeacherQueries, TeacherQueries>();
        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<ICarQueries, CarQueries>();
        services.AddScoped<IWeekScheduleRepository, WeekScheduleRepository>();
        services.AddScoped<IWeekScheduleQueries, WeekScheduleQueries>();
        services.AddScoped<IPublicationRepository, PublicationRepository>();
        services.AddScoped<IPublicationQueries, PublicationQueries>();
        services.AddScoped<ISubmissionQueries, SubmissionQueries>();
        services.AddScoped<IExcelGenerator, PlaceholderExcelGenerator>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<IPasswordVerifier, PasswordVerifier>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddSingleton(TimeProvider.System);

        services.AddQuartz();
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.AddTransient<OpenPublicationJob>();
        services.AddTransient<ClosePublicationJob>();
        services.AddScoped<IPublicationScheduler, PublicationScheduler>();

        services.AddHostedService<PublicationReconciliationHostedService>();

        return services;
    }
}
