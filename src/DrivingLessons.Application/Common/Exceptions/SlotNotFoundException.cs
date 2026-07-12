using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class SlotNotFoundException : NotFoundException
{
    public SlotNotFoundException(WeekScheduleId weekScheduleId, SlotId slotId)
        : base($"Slot {slotId.Value} was not found in week schedule {weekScheduleId.Value}.")
    {
    }
}
