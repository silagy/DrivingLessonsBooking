using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class WeekScheduleTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var weekSchedule = WeekSchedule.Create(teacher, weekStart);

        //then
        weekSchedule.TeacherId.ShouldBe(teacher.Id);
        weekSchedule.WeekStart.ShouldBe(weekStart);
        weekSchedule.Slots.ShouldAllBe(x => x.IsOpen);
    }

    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    public void Create__Weekday_Has_All_Four_Windows(DayOfWeek day)
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();

        //when
        var windows = weekSchedule.Slots
                                  .Where(x => x.Day == day)
                                  .Select(x => x.Window)
                                  .ToList();

        //then
        windows.ShouldBe(
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon,
                SlotWindowType.Afternoon,
                SlotWindowType.Evening
            ],
            ignoreOrder: true);
    }

    [TestMethod]
    public void Create__Friday_Has_Morning_And_Noon_Only()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();

        //when
        var windows = weekSchedule.Slots
                                  .Where(x => x.Day == DayOfWeek.Friday)
                                  .Select(x => x.Window)
                                  .ToList();

        //then
        windows.ShouldBe(
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon
            ],
            ignoreOrder: true);
    }

    [TestMethod]
    public void Create__Saturday_Does_Not_Exist()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();

        //when
        var saturdaySlots = weekSchedule.Slots.Where(x => x.Day == DayOfWeek.Saturday);

        //then
        saturdaySlots.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var weekSchedule = WeekSchedule.Create(teacher, weekStart);

        //then
        weekSchedule.UncommittedEvents
                    .OfType<WeekScheduleCreated>()
                    .Where(x => x.WeekScheduleId == weekSchedule.Id)
                    .Where(x => x.TeacherId == teacher.Id)
                    .Where(x => x.WeekStart == weekStart)
                    .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Mark_Slot_Unavailable()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();

        //when
        weekSchedule.MarkSlotUnavailable(slot);

        //then
        slot.IsUnavailable.ShouldBeTrue();
    }

    [TestMethod]
    public void Mark_Slot_Unavailable__Add_Event()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();

        //when
        weekSchedule.MarkSlotUnavailable(slot);

        //then
        weekSchedule.UncommittedEvents
                    .OfType<SlotMarkedUnavailable>()
                    .Where(x => x.WeekScheduleId == weekSchedule.Id)
                    .Where(x => x.SlotId == slot.Id)
                    .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Mark_Slot_Unavailable__Slot_Must_Be_Open()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        var act = () => weekSchedule.MarkSlotUnavailable(slot);

        //then
        Should.Throw<SlotMustBeOpenException>(act);
    }

    [TestMethod]
    public void Mark_Slot_Unavailable__Slot_Must_Be_In_Week_Schedule()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var otherWeekSchedule = WeekScheduleFakeBuilder.Build();
        var foreignSlot = otherWeekSchedule.Slots.First();

        //when
        var act = () => weekSchedule.MarkSlotUnavailable(foreignSlot);

        //then
        Should.Throw<SlotNotInWeekScheduleException>(act);
    }

    [TestMethod]
    public void Mark_Slot_Available()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        weekSchedule.MarkSlotAvailable(slot);

        //then
        slot.IsOpen.ShouldBeTrue();
    }

    [TestMethod]
    public void Mark_Slot_Available__Add_Event()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        weekSchedule.MarkSlotAvailable(slot);

        //then
        weekSchedule.UncommittedEvents
                    .OfType<SlotMarkedAvailable>()
                    .Where(x => x.WeekScheduleId == weekSchedule.Id)
                    .Where(x => x.SlotId == slot.Id)
                    .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Mark_Slot_Available__Slot_Must_Be_Unavailable()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();

        //when
        var act = () => weekSchedule.MarkSlotAvailable(slot);

        //then
        Should.Throw<SlotMustBeUnavailableException>(act);
    }

    [TestMethod]
    public void Mark_Slot_Available__Slot_Must_Be_In_Week_Schedule()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var otherWeekSchedule = WeekScheduleFakeBuilder.Build();
        var foreignSlot = otherWeekSchedule.Slots.First();

        //when
        var act = () => weekSchedule.MarkSlotAvailable(foreignSlot);

        //then
        Should.Throw<SlotNotInWeekScheduleException>(act);
    }
}
