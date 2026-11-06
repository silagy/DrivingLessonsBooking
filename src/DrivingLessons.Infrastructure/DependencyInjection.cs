using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.Csv;
using DrivingLessons.Infrastructure.DomainEvents;
using DrivingLessons.Infrastructure.Email;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.EntityFramework.Queries;
using DrivingLessons.Infrastructure.EntityFramework.Repositories;
using DrivingLessons.Infrastructure.Excel;
using DrivingLessons.Infrastructure.Options;
using DrivingLessons.Infrastructure.Outbox;
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
        services.AddScoped<ITeacherAndCarLocks, TeacherAndCarLocks>();
        services.AddScoped<IOutbox, Outbox.Outbox>();

        services.AddHealthChecks()
                .AddDbContextCheck<DrivingLessonsDbContext>();

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

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ITeacherQueries, TeacherQueries>();
        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<ICarQueries, CarQueries>();
        services.AddScoped<IWeekScheduleRepository, WeekScheduleRepository>();
        services.AddScoped<IWeekScheduleQueries, WeekScheduleQueries>();
        services.AddScoped<IPublicationRepository, PublicationRepository>();
        services.AddScoped<IPublicationQueries, PublicationQueries>();
        services.AddScoped<ISubmissionQueries, SubmissionQueries>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentQueries, StudentQueries>();
        services.AddScoped<IRosterImportRepository, RosterImportRepository>();
        services.AddScoped<IRosterImportQueries, RosterImportQueries>();
        services.AddScoped<IRosterCsvParser, RosterCsvParser>();
        services.AddScoped<IExcelGenerator, ExcelGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddSingleton(TimeProvider.System);

        var outboxSection = configuration.GetSection(OutboxOptions.SectionName);
        var outboxOptions = outboxSection.Get<OutboxOptions>() ?? new OutboxOptions();

        services.AddQuartz(quartz =>
        {
            quartz.ScheduleJob<ProcessOutboxJob>(
                trigger => trigger
                           .WithIdentity("process-outbox", ProcessOutboxJob.JobGroup)
                           .StartNow()
                           .WithSimpleSchedule(schedule => schedule
                                                           .WithIntervalInSeconds(outboxOptions.PollSeconds)
                                                           .RepeatForever()),
                job => job.WithIdentity("process-outbox", ProcessOutboxJob.JobGroup));

            quartz.ScheduleJob<PurgeOutboxJob>(
                trigger => trigger
                           .WithIdentity("purge-outbox", ProcessOutboxJob.JobGroup)
                           .StartNow()
                           .WithSimpleSchedule(schedule => schedule
                                                           .WithIntervalInHours(1)
                                                           .RepeatForever()),
                job => job.WithIdentity("purge-outbox", ProcessOutboxJob.JobGroup));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.AddTransient<OpenPublicationJob>();
        services.AddTransient<ClosePublicationJob>();
        services.AddScoped<OutboxProcessor>();
        services.AddTransient<ProcessOutboxJob>();
        services.AddTransient<PurgeOutboxJob>();
        services.AddScoped<IPublicationScheduler, PublicationScheduler>();

        services.AddHostedService<PublicationReconciliationHostedService>();

        return services;
    }
}
