using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SlotNotInWeekScheduleException : DomainException
{
    public SlotNotInWeekScheduleException(WeekScheduleId weekScheduleId, SlotId slotId)
        : base($"Slot {slotId.Value} is not in week schedule {weekScheduleId.Value}.")
    {
    }
}
