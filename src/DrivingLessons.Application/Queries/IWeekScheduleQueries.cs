using DrivingLessons.Application.Queries.GetWeekSchedule;

namespace DrivingLessons.Application.Queries;

public interface IWeekScheduleQueries
{
    Task<GetWeekScheduleResponse?> GetByTeacherAndWeekAsync(Guid teacherId, DateOnly weekStart);

    Task<IReadOnlyList<TeacherWeekGrid>> FindByWeekAsync(DateOnly weekStart);
}

public record TeacherWeekGrid(
    Guid TeacherId,
    string TeacherName,
    IReadOnlyCollection<SlotForGetWeekScheduleResponse> Slots);
