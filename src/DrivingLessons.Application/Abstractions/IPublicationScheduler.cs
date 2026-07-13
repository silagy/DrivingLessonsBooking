namespace DrivingLessons.Application.Abstractions;

public interface IPublicationScheduler
{
    Task SchedulePublicationJobsAsync(Guid publicationId, DateTimeOffset startUtc, DateTimeOffset endUtc);

    Task RescheduleCloseAsync(Guid publicationId, DateTimeOffset endUtc);
}
