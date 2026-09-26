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

    [TestMethod]
    [DataRow("2026-06-14", 25)]
    [DataRow("2026-12-27", 53)]
    [DataRow("2027-01-03", 1)]
    [DataRow("2025-12-28", 1)]
    public void Week_Number_Is_The_Iso_Week_Of_Its_Monday(string sunday, int expectedWeekNumber)
    {
        //given
        var weekStart = WeekStart.Of(DateOnly.Parse(sunday));

        //when
        var weekNumber = weekStart.WeekNumber;

        //then
        weekNumber.ShouldBe(expectedWeekNumber);
    }
}
