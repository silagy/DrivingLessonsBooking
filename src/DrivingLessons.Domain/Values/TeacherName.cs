using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record TeacherName
{
    public string Value { get; }

    private TeacherName(string value)
    {
        Value = value;
    }

    public static TeacherName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TeacherNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new TeacherName(normalized);
    }
}
