using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TargetSessionCountTest
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(40)]
    public void Target_Count_Is_Any_Positive_Number(int value)
    {
        //when
        var targetCount = TargetSessionCount.Of(value);

        //then
        targetCount.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Target_Count_Must_Be_Positive(int value)
    {
        //when
        var act = () => TargetSessionCount.Of(value);

        //then
        Should.Throw<TargetSessionCountMustBePositiveException>(act);
    }

    [TestMethod]
    [DataRow(2, 2)]
    [DataRow(2, 5)]
    public void Target_Is_Covered_By_At_Least_As_Many_Picks(int target, int pickCount)
    {
        //given
        var targetCount = TargetSessionCount.Of(target);

        //when
        var isCovered = targetCount.IsCoveredBy(pickCount);

        //then
        isCovered.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(2, 1)]
    [DataRow(3, 0)]
    [DataRow(30, 22)]
    public void Target_Is_Not_Covered_By_Fewer_Picks(int target, int pickCount)
    {
        //given
        var targetCount = TargetSessionCount.Of(target);

        //when
        var isCovered = targetCount.IsCoveredBy(pickCount);

        //then
        isCovered.ShouldBeFalse();
    }
}
