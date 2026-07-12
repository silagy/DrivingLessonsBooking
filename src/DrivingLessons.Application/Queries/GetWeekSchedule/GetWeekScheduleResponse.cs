using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleResponse
{
    public Guid Id { get; init; }
    public Guid TeacherId { get; init; }
    public DateOnly WeekStart { get; init; }
    public IReadOnlyCollection<SlotForGetWeekScheduleResponse> Slots { get; init; } = [];

    public static Expression<Func<WeekSchedule, GetWeekScheduleResponse>> Selector =>
        x => new GetWeekScheduleResponse
        {
            Id = x.Id.Value,
            TeacherId = x.TeacherId.Value,
            WeekStart = x.WeekStart.Value,
            Slots = x.Slots
                     .OrderBy(slot => slot.Day)
                     .ThenBy(slot => slot.Window)
                     .Select(slot => new SlotForGetWeekScheduleResponse
                     {
                         Id = slot.Id.Value,
                         Day = slot.Day,
                         Window = slot.Window,
                         State = slot.State
                     })
                     .ToList()
        };
}

public class SlotForGetWeekScheduleResponse
{
    public Guid Id { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public TimeOnly StartLocal => SlotWindowTimes.StartOf(Window);
    public TimeOnly EndLocal => SlotWindowTimes.EndOf(Window);
}
