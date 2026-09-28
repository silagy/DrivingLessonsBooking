using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SlotConstraintMustNotBeEmptyException : DomainException
{
    public SlotConstraintMustNotBeEmptyException()
        : base("Slot constraint must not be empty.")
    {
    }
}
