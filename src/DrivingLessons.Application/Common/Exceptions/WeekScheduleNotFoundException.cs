using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class WeekScheduleNotFoundException : NotFoundException
{
    public WeekScheduleNotFoundException(WeekScheduleId id)
        : base($"Week schedule {id.Value} was not found.")
    {
    }

    public WeekScheduleNotFoundException(Guid teacherId, DateOnly weekStart)
        : base($"Week schedule for teacher {teacherId} and week {weekStart:yyyy-MM-dd} was not found.")
    {
    }
}
