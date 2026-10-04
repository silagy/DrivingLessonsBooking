using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Password
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static Password Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PasswordMustNotBeEmptyException();
        }

        return new Password(value);
    }
}
