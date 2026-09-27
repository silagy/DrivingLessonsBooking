using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class RankTest
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    public void Rank_Is_Any_Positive_Position(int value)
    {
        //when
        var rank = Rank.Of(value);

        //then
        rank.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Rank_Must_Be_Positive(int value)
    {
        //when
        var act = () => Rank.Of(value);

        //then
        Should.Throw<RankMustBePositiveException>(act);
    }
}
