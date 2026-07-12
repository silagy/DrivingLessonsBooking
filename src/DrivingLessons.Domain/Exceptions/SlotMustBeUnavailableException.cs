using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SlotMustBeUnavailableException : DomainException
{
    public SlotMustBeUnavailableException(SlotId id)
        : base($"Slot {id.Value} must be unavailable.")
    {
    }
}
