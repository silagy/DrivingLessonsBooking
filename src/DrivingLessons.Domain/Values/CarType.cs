using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record CarType
{
    public string Value { get; }

    private CarType(string value)
    {
        Value = value;
    }

    public static CarType Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CarTypeMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new CarType(normalized);
    }
}
