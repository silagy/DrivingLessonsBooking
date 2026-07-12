using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class WeekStartTest
{
    [TestMethod]
    public void Of()
    {
        //given
        var sunday = new DateOnly(2026, 7, 19);

        //when
        var weekStart = WeekStart.Of(sunday);

        //then
        weekStart.Value.ShouldBe(sunday);
    }

    [TestMethod]
    [DataRow("2026-07-20")]
    [DataRow("2026-07-21")]
    [DataRow("2026-07-22")]
    [DataRow("2026-07-23")]
    [DataRow("2026-07-24")]
    [DataRow("2026-07-25")]
    public void Of__Must_Be_Sunday(string date)
    {
        //given
        var notSunday = DateOnly.Parse(date);

        //when
        var act = () => WeekStart.Of(notSunday);

        //then
        Should.Throw<WeekStartMustBeSundayException>(act);
    }
}
