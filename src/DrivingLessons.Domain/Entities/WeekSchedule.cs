using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class WeekSchedule : AggregateRoot<WeekScheduleId>
{
    private readonly List<Slot> slots = [];

    public TeacherId TeacherId { get; private set; }
    public WeekStart WeekStart { get; private set; }

    public IReadOnlyCollection<Slot> Slots => slots.AsReadOnly();

    private WeekSchedule()
    {
    }

    private WeekSchedule(WeekScheduleId id, TeacherId teacherId, WeekStart weekStart)
        : base(id)
    {
        TeacherId = teacherId;
        WeekStart = weekStart;

        var createdEvent = new WeekScheduleCreated(id, teacherId, weekStart);
        AddEvent(createdEvent);
    }

    public static WeekSchedule Create(Teacher teacher, WeekStart weekStart)
    {
        var id = WeekScheduleId.New();
        var weekSchedule = new WeekSchedule(id, teacher.Id, weekStart);
        weekSchedule.CreateOpenSlots();

        return weekSchedule;
    }

    public void MarkSlotUnavailable(Slot slot)
    {
        MustOwnSlot(slot);
        SlotMustBeOpen(slot);

        slot.MarkUnavailable();

        AddEvent(new SlotMarkedUnavailable(Id, slot.Id));
    }

    public void MarkSlotAvailable(Slot slot)
    {
        MustOwnSlot(slot);
        SlotMustBeUnavailable(slot);

        slot.MarkAvailable();

        AddEvent(new SlotMarkedAvailable(Id, slot.Id));
    }

    private void CreateOpenSlots()
    {
        foreach (var day in WeekGridDefinition.Days)
        {
            var windows = WeekGridDefinition.WindowsFor(day);

            foreach (var window in windows)
            {
                var slot = Slot.Create(day, window);
                slots.Add(slot);
            }
        }
    }

    private void MustOwnSlot(Slot slot)
    {
        var ownsSlot = slots.Contains(slot);

        if (!ownsSlot)
        {
            throw new SlotNotInWeekScheduleException(Id, slot.Id);
        }
    }

    private static void SlotMustBeOpen(Slot slot)
    {
        if (!slot.IsOpen)
        {
            throw new SlotMustBeOpenException(slot.Id);
        }
    }

    private static void SlotMustBeUnavailable(Slot slot)
    {
        if (!slot.IsUnavailable)
        {
            throw new SlotMustBeUnavailableException(slot.Id);
        }
    }
}
