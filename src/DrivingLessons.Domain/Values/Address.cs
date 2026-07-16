using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Address
{
    public string Value { get; }

    private Address(string value)
    {
        Value = value;
    }

    public static Address Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AddressMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new Address(normalized);
    }
}
