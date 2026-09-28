using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class SlotRequest : Entity<SlotRequestId>
{
    public SlotId SlotId { get; private set; }
    public SessionType SessionType { get; private set; }
    public SlotConstraint? Constraint { get; private set; }
    public Rank Rank { get; private set; }

    private SlotRequest()
    {
    }

    private SlotRequest(
        SlotRequestId id,
        SlotId slotId,
        SessionType sessionType,
        SlotConstraint? constraint,
        Rank rank)
        : base(id)
    {
        SlotId = slotId;
        SessionType = sessionType;
        Constraint = constraint;
        Rank = rank;
    }

    internal static SlotRequest Create(SlotPick pick, Rank rank)
    {
        var id = SlotRequestId.New();
        var slot = pick.Slot;

        return new SlotRequest(id, slot.Id, pick.SessionType, pick.Constraint, rank);
    }
}
