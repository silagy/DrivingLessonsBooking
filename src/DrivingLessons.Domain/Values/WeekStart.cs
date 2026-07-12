using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record WeekStart
{
    public DateOnly Value { get; }

    private WeekStart(DateOnly value)
    {
        Value = value;
    }

    public static WeekStart Of(DateOnly value)
    {
        if (value.DayOfWeek is not DayOfWeek.Sunday)
        {
            throw new WeekStartMustBeSundayException(value);
        }

        return new WeekStart(value);
    }
}
