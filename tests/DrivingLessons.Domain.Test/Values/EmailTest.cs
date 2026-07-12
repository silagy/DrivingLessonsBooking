using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class EmailTest
{
    [TestMethod]
    public void Email_Is_Normalized_To_Lower_Case()
    {
        //when
        var email = Email.Of(" Noa@Example.com ");

        //then
        email.Value.ShouldBe("noa@example.com");
    }

    [TestMethod]
    public void Equal_Concepts_Are_Equal_Instances()
    {
        //given
        var first = Email.Of("Noa@Example.com");
        var second = Email.Of("noa@example.com");

        //expected
        first.ShouldBe(second);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not-an-email")]
    [DataRow("a b@example.com")]
    public void Email_Must_Be_Valid(string? value)
    {
        //when
        var act = () => Email.Of(value!);

        //then
        Should.Throw<EmailMustBeValidException>(act);
    }
}
