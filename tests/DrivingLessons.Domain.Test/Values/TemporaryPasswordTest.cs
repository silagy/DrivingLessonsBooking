using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TemporaryPasswordTest
{
    [TestMethod]
    public void Password_Keeps_Its_Surrounding_Spaces()
    {
        //given
        var raw = $" {Faker.FakeString()} ";

        //when
        var password = TemporaryPassword.Of(raw);

        //then
        password.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Password_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => TemporaryPassword.Of(value!);

        //then
        Should.Throw<TemporaryPasswordMustNotBeEmptyException>(act);
    }
}
