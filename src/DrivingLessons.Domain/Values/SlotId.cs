using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record SlotId : EntityId
{
    private SlotId(Guid value)
        : base(value)
    {
    }

    public static SlotId New()
    {
        return new SlotId(Guid.NewGuid());
    }

    public static SlotId Of(Guid value)
    {
        return new SlotId(value);
    }
}
