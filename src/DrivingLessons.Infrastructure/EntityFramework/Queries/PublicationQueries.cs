using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class PublicationQueries : IPublicationQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public PublicationQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetPublicationResponse?> GetByWeekAsync(DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        return await dbContext
                         .Publications
                         .Where(x => x.WeekStart == resolvedWeekStart)
                         .Select(GetPublicationResponse.Selector)
                         .FirstOrDefaultAsync();
    }

    public async Task<GetPublicationDashboardResponse?> GetDashboardAsync(Guid publicationId, Guid teacherId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        var header = await dbContext
                             .Publications
                             .Where(x => x.Id == resolvedPublicationId)
                             .Select(x => new
                             {
                                 x.State,
                                 WindowStartUtc = (DateTimeOffset?)x.Window!.StartUtc,
                                 WindowEndUtc = (DateTimeOffset?)x.Window!.EndUtc,
                                 LinkToken = x.LinkToken.Value,
                                 WeekStart = x.WeekStart,
                                 LatestExcelVersion = x.TeacherVersions
                                                       .Where(v => v.TeacherId == resolvedTeacherId)
                                                       .Select(v => (int?)v.Version)
                                                       .FirstOrDefault()
                             })
                             .FirstOrDefaultAsync();

        if (header is null)
        {
            return null;
        }

        var slotCounts = await dbContext
                                 .WeekSchedules
                                 .Where(x => x.TeacherId == resolvedTeacherId && x.WeekStart == header.WeekStart)
                                 .SelectMany(x => x.Slots)
                                 .OrderBy(slot => slot.Day)
                                 .ThenBy(slot => slot.Window)
                                 .Select(slot => new SlotCountForGetPublicationDashboardResponse
                                 {
                                     SlotId = slot.Id.Value,
                                     Day = slot.Day,
                                     Window = slot.Window,
                                     State = slot.State,
                                     RequestCount = 0
                                 })
                                 .ToListAsync();

        return new GetPublicationDashboardResponse
        {
            State = header.State,
            WindowStartUtc = header.WindowStartUtc,
            WindowEndUtc = header.WindowEndUtc,
            LinkToken = header.LinkToken,
            StudentsSubmitted = 0,
            TotalPicks = 0,
            LastSubmissionAtUtc = null,
            LatestExcelVersion = header.LatestExcelVersion,
            SlotCounts = slotCounts
        };
    }

    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync()
    {
        var query = from publication in dbContext.Publications
                    from weekSchedule in dbContext.WeekSchedules
                        .Where(ws => ws.WeekStart == publication.WeekStart)
                    join teacher in dbContext.Teachers
                        on weekSchedule.TeacherId equals teacher.Id
                    orderby publication.WeekStart descending, teacher.Name
                    select new ItemForFindPublicationHistoryResponse
                    {
                        PublicationId = publication.Id.Value,
                        WeekStart = publication.WeekStart.Value,
                        TeacherId = teacher.Id.Value,
                        TeacherName = teacher.Name.Value,
                        State = publication.State,
                        WindowStartUtc = (DateTimeOffset?)publication.Window!.StartUtc,
                        WindowEndUtc = (DateTimeOffset?)publication.Window!.EndUtc,
                        LatestExcelVersion = publication.TeacherVersions
                                                        .Where(v => v.TeacherId == teacher.Id)
                                                        .Select(v => (int?)v.Version)
                                                        .FirstOrDefault()
                    };

        return await query.ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetTeacherIdsWithScheduleForWeekAsync(DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        return await dbContext
                         .WeekSchedules
                         .Where(x => x.WeekStart == resolvedWeekStart)
                         .Select(x => x.TeacherId.Value)
                         .ToListAsync();
    }
}
