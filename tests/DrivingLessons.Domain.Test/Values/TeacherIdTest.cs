using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TeacherIdTest
{
    [TestMethod]
    public void New_Ids_Are_Unique()
    {
        //given
        var first = TeacherId.New();
        var second = TeacherId.New();

        //expected
        first.ShouldNotBe(second);
    }

    [TestMethod]
    public void Of_Wraps_The_Value()
    {
        //given
        var value = Guid.NewGuid();

        //when
        var id = TeacherId.Of(value);

        //then
        id.Value.ShouldBe(value);
    }

    [TestMethod]
    public void Id_Must_Not_Be_Empty()
    {
        //when
        var act = () => TeacherId.Of(Guid.Empty);

        //then
        Should.Throw<ArgumentException>(act);
    }
}
