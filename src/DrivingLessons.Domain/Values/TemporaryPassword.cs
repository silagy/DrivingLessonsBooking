using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record TemporaryPassword
{
    public string Value { get; }

    private TemporaryPassword(string value)
    {
        Value = value;
    }

    public static TemporaryPassword Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TemporaryPasswordMustNotBeEmptyException();
        }

        return new TemporaryPassword(value);
    }
}
