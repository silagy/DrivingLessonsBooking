using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PasswordHashMustNotBeEmptyException();
        }

        return new PasswordHash(value);
    }
}
