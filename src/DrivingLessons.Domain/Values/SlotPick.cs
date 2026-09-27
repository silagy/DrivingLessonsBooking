using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SlotPick
{
    public Slot Slot { get; }
    public SessionType SessionType { get; }
    public SlotConstraint? Constraint { get; }

    private SlotPick(Slot slot, SessionType sessionType, SlotConstraint? constraint)
    {
        Slot = slot;
        SessionType = sessionType;
        Constraint = constraint;
    }

    public static SlotPick Of(Slot slot, SessionType sessionType, SlotConstraint? constraint)
    {
        if (!Enum.IsDefined(sessionType))
        {
            throw new SessionTypeMustBeSingleOrDoubleException();
        }

        return new SlotPick(slot, sessionType, constraint);
    }
}
