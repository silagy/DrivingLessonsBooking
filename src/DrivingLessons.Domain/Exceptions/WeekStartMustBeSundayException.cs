using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class WeekStartMustBeSundayException : DomainException
{
    public WeekStartMustBeSundayException(DateOnly value)
        : base($"Week start {value:yyyy-MM-dd} must be a Sunday.")
    {
    }
}
