using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record StudentName
{
    public string Value { get; }

    private StudentName(string value)
    {
        Value = value;
    }

    public static StudentName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new StudentNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new StudentName(normalized);
    }
}
