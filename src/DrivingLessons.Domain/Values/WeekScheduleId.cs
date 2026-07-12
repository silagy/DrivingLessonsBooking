using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record WeekScheduleId : EntityId
{
    private WeekScheduleId(Guid value)
        : base(value)
    {
    }

    public static WeekScheduleId New()
    {
        return new WeekScheduleId(Guid.NewGuid());
    }

    public static WeekScheduleId Of(Guid value)
    {
        return new WeekScheduleId(value);
    }
}
