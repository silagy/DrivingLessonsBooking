using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindSlotRequests;
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

    public async Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId)
    {
        var submissions = PublicationSubmissions(publicationId);

        return await CountsOf(submissions);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        return await CountsOf(submissions);
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

    public async Task<IReadOnlyList<SlotRequestDetail>> GetSlotRequestDetailsAsync(Guid publicationId)
    {
        var submissions = PublicationSubmissions(publicationId);

        return await DetailsOf(submissions);
    }

    public async Task<IReadOnlyList<SlotRequestDetail>> GetSlotRequestDetailsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        return await DetailsOf(submissions);
    }

    public async Task<IReadOnlyList<ItemForFindSlotRequestsResponse>> FindSlotRequestsAsync(
        Guid publicationId,
        Guid teacherId,
        Guid slotId)
    {
        var resolvedSlotId = SlotId.Of(slotId);
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var query = from submission in submissions
                    join student in dbContext.Students
                        on submission.StudentId equals student.Id
                    from request in submission.SlotRequests
                    where request.SlotId == resolvedSlotId
                    orderby submission.SubmittedAtUtc, student.Name, student.Id
                    select new
                    {
                        StudentId = student.Id,
                        StudentName = student.Name,
                        request.Rank,
                        PickCount = submission.SlotRequests.Count()
                    };

        var rows = await query.ToListAsync();

        return rows
                   .Select(x => new ItemForFindSlotRequestsResponse
                   {
                       StudentId = x.StudentId.Value,
                       StudentName = x.StudentName.Value,
                       Rank = x.Rank.Value,
                       PickCount = x.PickCount
                   })
                   .ToList();
    }

    private static async Task<IReadOnlyDictionary<Guid, int>> CountsOf(IQueryable<Submission> submissions)
    {
        var counts = await submissions
                               .SelectMany(x => x.SlotRequests)
                               .GroupBy(x => x.SlotId)
                               .Select(x => new { SlotId = x.Key, Count = x.Count() })
                               .ToListAsync();

        return counts.ToDictionary(x => x.SlotId.Value, x => x.Count);
    }

    private async Task<IReadOnlyList<SlotRequestDetail>> DetailsOf(IQueryable<Submission> submissions)
    {
        var query = from submission in submissions
                    join weekSchedule in dbContext.WeekSchedules
                        on submission.WeekScheduleId equals weekSchedule.Id
                    join teacher in dbContext.Teachers.IgnoreQueryFilters([QueryFilters.TeacherNotDeleted])
                        on weekSchedule.TeacherId equals teacher.Id
                    join student in dbContext.Students
                        on submission.StudentId equals student.Id
                    join car in dbContext.Cars.IgnoreQueryFilters([QueryFilters.CarNotDeleted])
                        on student.CarId equals car.Id
                    from request in submission.SlotRequests
                    from slot in weekSchedule.Slots
                    where slot.Id == request.SlotId
                    select new
                    {
                        SubmissionId = submission.Id,
                        slot.Day,
                        slot.Window,
                        StudentName = student.Name,
                        TeacherName = teacher.Name,
                        CarId = car.Id,
                        CarName = car.Name,
                        request.SessionType,
                        submission.TargetCount,
                        PickCount = submission.SlotRequests.Count(),
                        request.Constraint
                    };

        var rows = await query.ToListAsync();

        return rows
                   .Select(x => new SlotRequestDetail(
                       x.SubmissionId.Value,
                       x.Day,
                       x.Window,
                       x.StudentName.Value,
                       x.TeacherName.Value,
                       x.CarId.Value,
                       x.CarName.Value,
                       x.SessionType,
                       x.TargetCount.Value,
                       x.PickCount,
                       x.Constraint?.Value))
                   .ToList();
    }

    private IQueryable<Submission> PublicationSubmissions(Guid publicationId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);

        return dbContext
                   .Submissions
                   .Where(x => x.PublicationId == resolvedPublicationId);
    }

    private IQueryable<Submission> TeacherSubmissions(Guid publicationId, Guid teacherId)
    {
        var resolvedTeacherId = TeacherId.Of(teacherId);
        var submissions = PublicationSubmissions(publicationId);

        return from submission in submissions
               join weekSchedule in dbContext.WeekSchedules
                   on submission.WeekScheduleId equals weekSchedule.Id
               where weekSchedule.TeacherId == resolvedTeacherId
               select submission;
    }
}
