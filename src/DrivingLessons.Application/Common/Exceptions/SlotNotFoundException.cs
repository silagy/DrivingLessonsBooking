using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class SlotNotFoundException : NotFoundException
{
    public SlotNotFoundException()
        : base("The requested slot is not in the student's week schedule.")
    {
    }

    public SlotNotFoundException(WeekScheduleId weekScheduleId, SlotId slotId)
        : base($"Slot {slotId.Value} was not found in week schedule {weekScheduleId.Value}.")
    {
    }
}
