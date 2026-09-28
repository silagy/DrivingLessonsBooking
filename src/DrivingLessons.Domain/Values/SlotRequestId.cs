using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record SlotRequestId : EntityId
{
    private SlotRequestId(Guid value)
        : base(value)
    {
    }

    public static SlotRequestId New()
    {
        return new SlotRequestId(Guid.NewGuid());
    }

    public static SlotRequestId Of(Guid value)
    {
        return new SlotRequestId(value);
    }
}
