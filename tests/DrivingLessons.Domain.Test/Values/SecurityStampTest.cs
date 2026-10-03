using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SecurityStampTest
{
    [TestMethod]
    public void New_Produces_Non_Empty_Stamp()
    {
        //when
        var stamp = SecurityStamp.New();

        //then
        stamp.Value.ShouldNotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void New_Produces_Distinct_Stamps()
    {
        //when
        var first = SecurityStamp.New();
        var second = SecurityStamp.New();

        //then
        first.ShouldNotBe(second);
    }

    [TestMethod]
    public void Of()
    {
        //given
        var value = Faker.FakeString();

        //when
        var stamp = SecurityStamp.Of(value);

        //then
        stamp.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Of__Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => SecurityStamp.Of(value!);

        //then
        Should.Throw<SecurityStampMustNotBeEmptyException>(act);
    }
}
