using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DrivingLessons.Application.Test.Emails;

[TestClass]
public class EmailOptionsTest
{
    [TestMethod]
    public void Disabled_Email_Needs_No_Smtp_Settings()
    {
        //given
        var options = new EmailOptions();

        //when
        var result = Validate(options);

        //then
        result.Succeeded.ShouldBeTrue();
    }

    [TestMethod]
    public void Enabled_Email_With_Complete_Settings_Is_Valid()
    {
        //given
        var options = new EmailOptions
        {
            Enabled = true,
            Host = "email-smtp.eu-central-1.amazonaws.com",
            Username = "smtp-user",
            Password = "smtp-password",
            FromAddress = "noreply@school.example.com"
        };

        //when
        var result = Validate(options);

        //then
        result.Succeeded.ShouldBeTrue();
    }

    [TestMethod]
    public void Enabled_Email_Without_Credentials_Is_Valid()
    {
        //given
        var options = new EmailOptions
        {
            Enabled = true,
            Host = "localhost",
            FromAddress = "noreply@local.dev"
        };

        //when
        var result = Validate(options);

        //then
        result.Succeeded.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow("", "noreply@school.example.com", "", "", "Email host is required when email is enabled.")]
    [DataRow("smtp.example.com", "", "", "", "A valid from address is required when email is enabled.")]
    [DataRow("smtp.example.com", "not-an-address", "", "", "A valid from address is required when email is enabled.")]
    [DataRow(
        "smtp.example.com",
        "School <noreply@school.example.com>",
        "",
        "",
        "A valid from address is required when email is enabled.")]
    [DataRow(
        "smtp.example.com",
        "no reply@school.example.com",
        "",
        "",
        "A valid from address is required when email is enabled.")]
    [DataRow("smtp.example.com", "noreply@school.example.com", "smtp-user", "", "An email password is required with a username.")]
    public void Enabled_Email_Must_Have_Its_Smtp_Settings(
        string host,
        string fromAddress,
        string username,
        string password,
        string expectedFailure)
    {
        //given
        var options = new EmailOptions
        {
            Enabled = true,
            Host = host,
            Username = username,
            Password = password,
            FromAddress = fromAddress
        };

        //when
        var result = Validate(options);

        //then
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(expectedFailure);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(65536)]
    public void Port_Must_Be_A_Tcp_Port(int port)
    {
        //given
        var options = new EmailOptions
        {
            Port = port
        };

        //when
        var result = Validate(options);

        //then
        result.Failed.ShouldBeTrue();
    }

    private static ValidateOptionsResult Validate(EmailOptions options)
    {
        var validator = new DataAnnotationValidateOptions<EmailOptions>(string.Empty);

        return validator.Validate(string.Empty, options);
    }
}
