using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record CarName
{
    public string Value { get; }

    private CarName(string value)
    {
        Value = value;
    }

    public static CarName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CarNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new CarName(normalized);
    }
}
