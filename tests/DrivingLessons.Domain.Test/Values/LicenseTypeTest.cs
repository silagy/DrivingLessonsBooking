using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class LicenseTypeTest
{
    [TestMethod]
    public void License_Type_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var licenseType = LicenseType.Of($"  {raw}  ");

        //then
        licenseType.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void License_Type_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => LicenseType.Of(value!);

        //then
        Should.Throw<LicenseTypeMustNotBeEmptyException>(act);
    }
}
