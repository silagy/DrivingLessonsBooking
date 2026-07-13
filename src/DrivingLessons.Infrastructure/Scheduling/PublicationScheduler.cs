using DrivingLessons.Application.Abstractions;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

public class PublicationScheduler : IPublicationScheduler
{
    private const string JobGroup = "publications";

    private readonly ISchedulerFactory schedulerFactory;

    public PublicationScheduler(ISchedulerFactory schedulerFactory)
    {
        this.schedulerFactory = schedulerFactory;
    }

    public async Task SchedulePublicationJobsAsync(Guid publicationId, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var openJob = BuildJob<OpenPublicationJob>(publicationId, OpenKey(publicationId));
        var openTrigger = BuildTrigger(OpenKey(publicationId), startUtc);
        await scheduler.ScheduleJob(openJob, openTrigger);

        var closeJob = BuildJob<ClosePublicationJob>(publicationId, CloseKey(publicationId));
        var closeTrigger = BuildTrigger(CloseKey(publicationId), endUtc);
        await scheduler.ScheduleJob(closeJob, closeTrigger);
    }

    public async Task RescheduleCloseAsync(Guid publicationId, DateTimeOffset endUtc)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var closeKey = CloseKey(publicationId);
        var triggerKey = new TriggerKey(closeKey.Name, closeKey.Group);
        var newTrigger = BuildTrigger(closeKey, endUtc);

        var exists = await scheduler.CheckExists(triggerKey);

        if (exists)
        {
            await scheduler.RescheduleJob(triggerKey, newTrigger);
            return;
        }

        var closeJob = BuildJob<ClosePublicationJob>(publicationId, closeKey);
        await scheduler.ScheduleJob(closeJob, newTrigger);
    }

    private static IJobDetail BuildJob<TJob>(Guid publicationId, JobKey jobKey)
        where TJob : IJob
    {
        return JobBuilder
                   .Create<TJob>()
                   .WithIdentity(jobKey)
                   .UsingJobData(OpenPublicationJob.PublicationIdKey, publicationId.ToString())
                   .Build();
    }

    private static ITrigger BuildTrigger(JobKey jobKey, DateTimeOffset fireAtUtc)
    {
        return TriggerBuilder
                   .Create()
                   .WithIdentity(jobKey.Name, jobKey.Group)
                   .StartAt(fireAtUtc)
                   .Build();
    }

    private static JobKey OpenKey(Guid publicationId)
    {
        return new JobKey($"open-{publicationId}", JobGroup);
    }

    private static JobKey CloseKey(Guid publicationId)
    {
        return new JobKey($"close-{publicationId}", JobGroup);
    }
}
