using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class CarTypeTest
{
    [TestMethod]
    public void Type_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var type = CarType.Of($"  {raw}  ");

        //then
        type.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Type_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => CarType.Of(value!);

        //then
        Should.Throw<CarTypeMustNotBeEmptyException>(act);
    }
}
