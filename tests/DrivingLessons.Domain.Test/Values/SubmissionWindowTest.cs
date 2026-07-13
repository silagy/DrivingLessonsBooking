using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SubmissionWindowTest
{
    [TestMethod]
    public void Of()
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddDays(3);

        //when
        var window = SubmissionWindow.Of(startUtc, endUtc);

        //then
        window.StartUtc.ShouldBe(startUtc);
        window.EndUtc.ShouldBe(endUtc);
    }

    [TestMethod]
    public void Of__End_Must_Be_After_Start_When_Equal()
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc;

        //when
        var act = () => SubmissionWindow.Of(startUtc, endUtc);

        //then
        Should.Throw<SubmissionWindowEndMustBeAfterStartException>(act);
    }

    [TestMethod]
    public void Of__End_Must_Be_After_Start_When_Earlier()
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddSeconds(-1);

        //when
        var act = () => SubmissionWindow.Of(startUtc, endUtc);

        //then
        Should.Throw<SubmissionWindowEndMustBeAfterStartException>(act);
    }
}
