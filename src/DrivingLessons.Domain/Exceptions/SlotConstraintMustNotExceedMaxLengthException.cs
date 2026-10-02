using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SlotConstraintMustNotExceedMaxLengthException : DomainException
{
    public int MaxLength { get; }

    public SlotConstraintMustNotExceedMaxLengthException(int maxLength)
        : base($"Slot constraint must be at most {maxLength} characters.")
    {
        MaxLength = maxLength;
    }
}
