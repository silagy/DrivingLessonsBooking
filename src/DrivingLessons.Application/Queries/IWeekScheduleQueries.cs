using DrivingLessons.Application.Queries.GetWeekSchedule;

namespace DrivingLessons.Application.Queries;

public interface IWeekScheduleQueries
{
    Task<GetWeekScheduleResponse?> GetByTeacherAndWeekAsync(Guid teacherId, DateOnly weekStart);
}
