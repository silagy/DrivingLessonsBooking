using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SlotConstraintTest
{
    [TestMethod]
    public void Constraint_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var constraint = SlotConstraint.Of($"  {raw}  ");

        //then
        constraint.Value.ShouldBe(raw);
    }

    [TestMethod]
    public void Constraint_May_Be_Exactly_The_Max_Length()
    {
        //given
        var raw = new string('a', SlotConstraint.MaxLength);

        //when
        var constraint = SlotConstraint.Of(raw);

        //then
        constraint.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constraint_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => SlotConstraint.Of(value!);

        //then
        Should.Throw<SlotConstraintMustNotBeEmptyException>(act);
    }

    [TestMethod]
    public void Constraint_Must_Not_Exceed_The_Max_Length()
    {
        //given
        var raw = new string('a', SlotConstraint.MaxLength + 1);

        //when
        var act = () => SlotConstraint.Of(raw);

        //then
        Should.Throw<SlotConstraintMustNotExceedMaxLengthException>(act);
    }

    [TestMethod]
    public void Too_Long_Message_Does_Not_Reveal_The_Constraint()
    {
        //given
        var raw = $"{Faker.FakeString()}{new string('a', SlotConstraint.MaxLength)}";

        //when
        var act = () => SlotConstraint.Of(raw);

        //then
        var exception = Should.Throw<SlotConstraintMustNotExceedMaxLengthException>(act);
        exception.Message.ShouldNotContain(raw[..20]);
    }
}
