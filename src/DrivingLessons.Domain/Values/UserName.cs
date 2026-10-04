using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record UserName
{
    public string Value { get; }

    private UserName(string value)
    {
        Value = value;
    }

    public static UserName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UserNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new UserName(normalized);
    }
}
