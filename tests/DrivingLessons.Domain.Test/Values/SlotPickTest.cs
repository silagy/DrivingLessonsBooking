using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SlotPickTest
{
    [TestMethod]
    public void Pick_Keeps_Slot_Session_Type_And_Constraint()
    {
        //given
        var slot = WeekScheduleFakeBuilder.Build().Slots.First();
        var constraint = SlotConstraint.Of(Faker.FakeString());

        //when
        var pick = SlotPick.Of(slot, SessionType.Double, constraint);

        //then
        pick.Slot.ShouldBe(slot);
        pick.SessionType.ShouldBe(SessionType.Double);
        pick.Constraint.ShouldBe(constraint);
    }

    [TestMethod]
    public void Constraint_Is_Optional()
    {
        //given
        var slot = WeekScheduleFakeBuilder.Build().Slots.First();

        //when
        var pick = SlotPick.Of(slot, SessionType.Single, null);

        //then
        pick.Constraint.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(15)]
    [DataRow(99)]
    public void Session_Type_Must_Be_Single_Or_Double(int value)
    {
        //given
        var slot = WeekScheduleFakeBuilder.Build().Slots.First();
        var sessionType = (SessionType)value;

        //when
        var act = () => SlotPick.Of(slot, sessionType, null);

        //then
        Should.Throw<SessionTypeMustBeSingleOrDoubleException>(act);
    }
}
