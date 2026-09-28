using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SlotConstraintMustNotExceedMaxLengthException : DomainException
{
    public SlotConstraintMustNotExceedMaxLengthException(int maxLength)
        : base($"Slot constraint must be at most {maxLength} characters.")
    {
    }
}
