using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class PasswordHashTest
{
    [TestMethod]
    public void Of()
    {
        //given
        var value = Faker.FakeString();

        //when
        var hash = PasswordHash.Of(value);

        //then
        hash.Value.ShouldBe(value);
    }

    [TestMethod]
    public void Hash_Is_Kept_Exactly_As_Given()
    {
        //given
        var value = $" {Faker.FakeString()} ";

        //when
        var hash = PasswordHash.Of(value);

        //then
        hash.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Hash_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => PasswordHash.Of(value!);

        //then
        Should.Throw<PasswordHashMustNotBeEmptyException>(act);
    }
}
