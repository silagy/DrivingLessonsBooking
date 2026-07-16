using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class RosterFileNameTest
{
    [TestMethod]
    public void File_Name_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var name = RosterFileName.Of($"  {raw}  ");

        //then
        name.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void File_Name_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => RosterFileName.Of(value!);

        //then
        Should.Throw<RosterFileNameMustNotBeEmptyException>(act);
    }
}
