using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SlotConstraint
{
    public const int MaxLength = 200;

    public string Value { get; }

    private SlotConstraint(string value)
    {
        Value = value;
    }

    public static SlotConstraint Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SlotConstraintMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            throw new SlotConstraintMustNotExceedMaxLengthException(MaxLength);
        }

        return new SlotConstraint(normalized);
    }
}
