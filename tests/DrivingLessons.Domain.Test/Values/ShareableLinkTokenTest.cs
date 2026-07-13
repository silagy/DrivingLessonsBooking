using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class ShareableLinkTokenTest
{
    [TestMethod]
    public void New_Produces_Non_Empty_Token()
    {
        //when
        var token = ShareableLinkToken.New();

        //then
        token.Value.ShouldNotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void New_Produces_Distinct_Tokens()
    {
        //when
        var first = ShareableLinkToken.New();
        var second = ShareableLinkToken.New();

        //then
        first.Value.ShouldNotBe(second.Value);
    }

    [TestMethod]
    public void Of()
    {
        //given
        var value = Faker.FakeString();

        //when
        var token = ShareableLinkToken.Of(value);

        //then
        token.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Of__Must_Not_Be_Empty(string value)
    {
        //when
        var act = () => ShareableLinkToken.Of(value);

        //then
        Should.Throw<ShareableLinkTokenMustNotBeEmptyException>(act);
    }
}
