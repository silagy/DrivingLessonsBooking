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

    public async Task<IReadOnlyList<TeacherWeekGrid>> FindByWeekAsync(DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        var schedules = await dbContext
                                .WeekSchedules
                                .Where(x => x.WeekStart == resolvedWeekStart)
                                .Select(GetWeekScheduleResponse.Selector)
                                .ToListAsync();

        var teacherQuery = from weekSchedule in dbContext.WeekSchedules
                           join teacher in dbContext.Teachers.IgnoreQueryFilters([QueryFilters.TeacherNotDeleted])
                               on weekSchedule.TeacherId equals teacher.Id
                           where weekSchedule.WeekStart == resolvedWeekStart
                           select new { teacher.Id, teacher.Name };

        var teachers = await teacherQuery.ToListAsync();

        var teacherNames = teachers.ToDictionary(x => x.Id.Value, x => x.Name.Value);

        return schedules
                   .Select(x => new TeacherWeekGrid(x.TeacherId, teacherNames[x.TeacherId], x.Slots))
                   .ToList();
    }
}
