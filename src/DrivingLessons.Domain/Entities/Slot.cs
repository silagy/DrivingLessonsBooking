using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Slot : Entity<SlotId>
{
    public DayOfWeek Day { get; private set; }
    public SlotWindowType Window { get; private set; }
    public SlotState State { get; private set; }

    public bool IsOpen => State is SlotState.Open;
    public bool IsUnavailable => State is SlotState.Unavailable;

    private Slot()
    {
    }

    private Slot(SlotId id, DayOfWeek day, SlotWindowType window, SlotState state)
        : base(id)
    {
        Day = day;
        Window = window;
        State = state;
    }

    internal static Slot Create(DayOfWeek day, SlotWindowType window)
    {
        const SlotState state = SlotState.Open;
        var id = SlotId.New();

        return new Slot(id, day, window, state);
    }

    internal void MarkUnavailable()
    {
        State = SlotState.Unavailable;
    }

    internal void MarkAvailable()
    {
        State = SlotState.Open;
    }
}
