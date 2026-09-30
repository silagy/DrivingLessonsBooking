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

    public async Task<IReadOnlyList<SlotRequestDetail>> GetSlotRequestDetailsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var query = from submission in submissions
                    join weekSchedule in dbContext.WeekSchedules
                        on submission.WeekScheduleId equals weekSchedule.Id
                    join student in dbContext.Students
                        on submission.StudentId equals student.Id
                    join car in dbContext.Cars.IgnoreQueryFilters()
                        on student.CarId equals car.Id
                    from request in submission.SlotRequests
                    from slot in weekSchedule.Slots
                    where slot.Id == request.SlotId
                    select new
                    {
                        slot.Day,
                        slot.Window,
                        StudentName = student.Name,
                        student.NationalId,
                        student.Phone,
                        car.Transmission,
                        request.SessionType,
                        request.Rank,
                        submission.TargetCount,
                        request.Constraint
                    };

        var rows = await query.ToListAsync();

        return rows
                   .Select(x => new SlotRequestDetail(
                       x.Day,
                       x.Window,
                       x.StudentName.Value,
                       x.NationalId.Value,
                       x.Phone.Value,
                       x.Transmission,
                       x.SessionType,
                       x.Rank.Value,
                       x.TargetCount.Value,
                       x.Constraint?.Value))
                   .ToList();
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
