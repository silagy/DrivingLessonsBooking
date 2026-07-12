using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IWeekScheduleRepository
{
    Task<WeekSchedule?> GetAsync(WeekScheduleId id);

    Task<WeekSchedule?> GetByTeacherAndWeekAsync(TeacherId teacherId, WeekStart weekStart);

    void Add(WeekSchedule weekSchedule);
}
