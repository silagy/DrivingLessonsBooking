using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class WeekScheduleQueries : IWeekScheduleQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public WeekScheduleQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetWeekScheduleResponse?> GetByTeacherAndWeekAsync(Guid teacherId, DateOnly weekStart)
    {
        var resolvedTeacherId = TeacherId.Of(teacherId);
        var resolvedWeekStart = WeekStart.Of(weekStart);

        return await dbContext
                         .WeekSchedules
                         .Where(x => x.TeacherId == resolvedTeacherId && x.WeekStart == resolvedWeekStart)
                         .Select(GetWeekScheduleResponse.Selector)
                         .FirstOrDefaultAsync();
    }
}
