using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record RosterFileName
{
    public string Value { get; }

    private RosterFileName(string value)
    {
        Value = value;
    }

    public static RosterFileName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RosterFileNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new RosterFileName(normalized);
    }
}
