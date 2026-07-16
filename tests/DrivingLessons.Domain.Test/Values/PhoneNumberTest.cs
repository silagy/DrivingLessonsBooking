using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class PhoneNumberTest
{
    [TestMethod]
    public void Phone_Number_Strips_Directional_Marks_And_Whitespace()
    {
        //given
        var wrapped = "\u200F 052-1234567 \u200E";

        //when
        var phone = PhoneNumber.Of(wrapped);

        //then
        phone.Value.ShouldBe("052-1234567");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("054")]
    [DataRow("abc")]
    public void Must_Contain_At_Least_Seven_Digits(string value)
    {
        //when
        var act = () => PhoneNumber.Of(value);

        //then
        Should.Throw<PhoneNumberMustBeValidException>(act);
    }
}
