using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SlotWindowTimesTest
{
    [TestMethod]
    [DataRow(SlotWindowType.Morning, "07:00")]
    [DataRow(SlotWindowType.Noon, "12:00")]
    [DataRow(SlotWindowType.Afternoon, "15:00")]
    [DataRow(SlotWindowType.Evening, "18:00")]
    public void Start_Of(SlotWindowType window, string time)
    {
        //given
        var expectedStart = TimeOnly.Parse(time);

        //when
        var start = SlotWindowTimes.StartOf(window);

        //then
        start.ShouldBe(expectedStart);
    }

    [TestMethod]
    [DataRow(SlotWindowType.Morning, "12:00")]
    [DataRow(SlotWindowType.Noon, "15:00")]
    [DataRow(SlotWindowType.Afternoon, "18:00")]
    [DataRow(SlotWindowType.Evening, "22:00")]
    public void End_Of(SlotWindowType window, string time)
    {
        //given
        var expectedEnd = TimeOnly.Parse(time);

        //when
        var end = SlotWindowTimes.EndOf(window);

        //then
        end.ShouldBe(expectedEnd);
    }
}
