using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class WeekScheduleRepository : IWeekScheduleRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public WeekScheduleRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<WeekSchedule?> GetAsync(WeekScheduleId id)
    {
        return await dbContext.WeekSchedules.FindAsync(id);
    }

    public async Task<WeekSchedule?> GetByTeacherAndWeekAsync(TeacherId teacherId, WeekStart weekStart)
    {
        return await dbContext
                         .WeekSchedules
                         .FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.WeekStart == weekStart);
    }

    public void Add(WeekSchedule weekSchedule)
    {
        dbContext.WeekSchedules.Add(weekSchedule);
    }
}
