namespace DrivingLessons.Domain.Values;

public static class SlotWindowTimes
{
    public static TimeOnly StartOf(SlotWindowType window)
    {
        return window switch
        {
            SlotWindowType.Morning => new TimeOnly(7, 0),
            SlotWindowType.Noon => new TimeOnly(12, 0),
            SlotWindowType.Afternoon => new TimeOnly(15, 0),
            SlotWindowType.Evening => new TimeOnly(18, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(window))
        };
    }

    public static TimeOnly EndOf(SlotWindowType window)
    {
        return window switch
        {
            SlotWindowType.Morning => new TimeOnly(12, 0),
            SlotWindowType.Noon => new TimeOnly(15, 0),
            SlotWindowType.Afternoon => new TimeOnly(18, 0),
            SlotWindowType.Evening => new TimeOnly(22, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(window))
        };
    }
}
