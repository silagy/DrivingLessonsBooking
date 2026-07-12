using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class WeekScheduleAlreadyExistsException : DomainException
{
    public WeekScheduleAlreadyExistsException(TeacherId teacherId, WeekStart weekStart)
        : base($"Week schedule for teacher {teacherId.Value} and week {weekStart.Value:yyyy-MM-dd} already exists.")
    {
    }
}
