using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SlotMustBeOpenException : DomainException
{
    public SlotMustBeOpenException(SlotId id)
        : base($"Slot {id.Value} must be open.")
    {
    }
}
