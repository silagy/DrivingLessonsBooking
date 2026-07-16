using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class AddressTest
{
    [TestMethod]
    public void Address_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var address = Address.Of($"  {raw}  ");

        //then
        address.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Address_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => Address.Of(value!);

        //then
        Should.Throw<AddressMustNotBeEmptyException>(act);
    }
}
