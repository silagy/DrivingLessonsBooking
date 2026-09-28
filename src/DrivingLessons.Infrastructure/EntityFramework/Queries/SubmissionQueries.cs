using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class SubmissionQueries : ISubmissionQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public SubmissionQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var counts = await submissions
                               .SelectMany(x => x.SlotRequests)
                               .GroupBy(x => x.SlotId)
                               .Select(x => new { SlotId = x.Key, Count = x.Count() })
                               .ToListAsync();

        return counts.ToDictionary(x => x.SlotId.Value, x => x.Count);
    }

    public async Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var studentsSubmitted = await submissions.CountAsync();

        var totalPicks = await submissions
                                   .SelectMany(x => x.SlotRequests)
                                   .CountAsync();

        var lastSubmissionAtUtc = await submissions.MaxAsync(x => (DateTimeOffset?)(x.RevisedAtUtc ?? x.SubmittedAtUtc));

        return new SubmissionStats(studentsSubmitted, totalPicks, lastSubmissionAtUtc);
    }

    private IQueryable<Submission> TeacherSubmissions(Guid publicationId, Guid teacherId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        return from submission in dbContext.Submissions
               join weekSchedule in dbContext.WeekSchedules
                   on submission.WeekScheduleId equals weekSchedule.Id
               where submission.PublicationId == resolvedPublicationId
                     && weekSchedule.TeacherId == resolvedTeacherId
               select submission;
    }
}
