using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class StudentNameTest
{
    [TestMethod]
    public void Name_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var name = StudentName.Of($"  {raw}  ");

        //then
        name.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Name_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => StudentName.Of(value!);

        //then
        Should.Throw<StudentNameMustNotBeEmptyException>(act);
    }
}
