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

    [TestMethod]
    [DataRow("050-1234567", "972501234567")]
    [DataRow("0501234567", "972501234567")]
    [DataRow("054 123 4567", "972541234567")]
    [DataRow("(052) 123-4567", "972521234567")]
    [DataRow("+972-50-1234567", "972501234567")]
    [DataRow("+972 50 123 4567", "972501234567")]
    [DataRow("972501234567", "972501234567")]
    [DataRow("00972501234567", "972501234567")]
    [DataRow("050.123.4567", "972501234567")]
    [DataRow("‏050-1234567‎", "972501234567")]
    public void Israeli_Mobile_Converts_To_WhatsApp_Number(string value, string expected)
    {
        //given
        var phone = PhoneNumber.Of(value);

        //when
        var whatsAppNumber = phone.ToWhatsAppNumber();

        //then
        whatsAppNumber.ShouldNotBeNull();
        whatsAppNumber.Value.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("02-6234567")]
    [DataRow("03 1234567")]
    [DataRow("050-123456")]
    [DataRow("050-12345678")]
    [DataRow("+44 7911 123456")]
    [DataRow("+050-1234567")]
    [DataRow("050-1234567 / 052-7654321")]
    [DataRow("050-1234567 ext 2")]
    [DataRow("1-800-123-4567")]
    public void Other_Phone_Has_No_WhatsApp_Number(string value)
    {
        //given
        var phone = PhoneNumber.Of(value);

        //when
        var whatsAppNumber = phone.ToWhatsAppNumber();

        //then
        whatsAppNumber.ShouldBeNull();
    }
}
