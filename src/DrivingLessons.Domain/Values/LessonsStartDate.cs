namespace DrivingLessons.Domain.Values;

public record LessonsStartDate
{
    public DateOnly Value { get; }

    private LessonsStartDate(DateOnly value)
    {
        Value = value;
    }

    public static LessonsStartDate Of(DateOnly value)
    {
        return new LessonsStartDate(value);
    }
}
