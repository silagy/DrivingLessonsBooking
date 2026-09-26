using System.Globalization;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record WeekStart
{
    private const int DaysFromSundayToMonday = 1;

    public DateOnly Value { get; }

    public int WeekNumber
    {
        get
        {
            var monday = Value.AddDays(DaysFromSundayToMonday);
            var mondayDateTime = monday.ToDateTime(TimeOnly.MinValue);

            return ISOWeek.GetWeekOfYear(mondayDateTime);
        }
    }

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
