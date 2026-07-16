using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class NationalIdTest
{
    [TestMethod]
    public void National_Id_Is_Zero_Padded_To_Nine_Digits()
    {
        //given
        var shortId = "1234566";

        //when
        var nationalId = NationalId.Of(shortId);

        //then
        nationalId.Value.ShouldBe("001234566");
    }

    [TestMethod]
    public void National_Id_Strips_Directional_Marks()
    {
        //given
        var wrapped = "\u200F123456782\u200E";

        //when
        var nationalId = NationalId.Of(wrapped);

        //then
        nationalId.Value.ShouldBe("123456782");
    }

    [TestMethod]
    [DataRow("12345678a")]
    [DataRow("123-45678")]
    [DataRow("")]
    [DataRow(" ")]
    public void Must_Contain_Only_Digits(string value)
    {
        //when
        var act = () => NationalId.Of(value);

        //then
        Should.Throw<NationalIdMustBeDigitsException>(act);
    }

    [TestMethod]
    [DataRow("1234567890")]
    public void Must_Be_At_Most_Nine_Digits(string value)
    {
        //when
        var act = () => NationalId.Of(value);

        //then
        Should.Throw<NationalIdMustBeAtMostNineDigitsException>(act);
    }

    [TestMethod]
    public void Must_Have_Valid_Check_Digit()
    {
        //given
        var valid = Faker.FakeNationalId().Value;
        var lastDigit = valid[^1] - '0';
        var flippedDigit = (lastDigit + 1) % 10;
        var invalid = valid[..8] + flippedDigit;

        //when
        var act = () => NationalId.Of(invalid);

        //then
        Should.Throw<NationalIdMustHaveValidCheckDigitException>(act);
    }

    [TestMethod]
    public void Equal_National_Ids_With_Different_Padding_Are_Equal()
    {
        //given
        var shortForm = NationalId.Of("1234566");
        var paddedForm = NationalId.Of("001234566");

        //expected
        shortForm.ShouldBe(paddedForm);
    }
}
