namespace DrivingLessons.Domain.Values;

public static class WeekGridDefinition
{
    public static readonly IReadOnlyList<DayOfWeek> Days =
    [
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    public static IReadOnlyList<SlotWindowType> WindowsFor(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Saturday => [],
            DayOfWeek.Friday =>
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon
            ],
            _ =>
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon,
                SlotWindowType.Afternoon,
                SlotWindowType.Evening
            ]
        };
    }
}
