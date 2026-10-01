using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Email;
using DrivingLessons.Infrastructure.Options;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace DrivingLessons.Application.Test.Emails;

[TestClass]
public class SmtpEmailSenderTest
{
    private const string ToEmail = "moshe.cohen@example.com";
    private const string TeacherName = "משה כהן";
    private const string Subject = "בקשות לשבוע 41 - משה כהן - גרסה 2";

    [TestMethod]
    public async Task Disabled_Email_Logs_The_Skip_Without_The_Teachers_Contact()
    {
        //given
        var logger = A.Fake<ILogger<SmtpEmailSender>>();
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions());
        var sender = new SmtpEmailSender(options, logger);
        byte[] content =
        [
            80,
            75,
            3,
            4
        ];
        var attachment = new ExcelFile($"{Subject}.xlsx", content, "application/octet-stream");
        var message = new EmailMessage(ToEmail, Subject, $"שלום {TeacherName},", attachment);

        //when
        await sender.SendAsync(message);

        //then
        var logged = Fake.GetCalls(logger)
                         .Where(x => x.Method.Name == nameof(ILogger.Log))
                         .Select(x => x.Arguments[2]!.ToString()!)
                         .ShouldHaveSingleItem();
        logged.ShouldStartWith("Email disabled. Skipped send");
        logged.ShouldNotContain(ToEmail);
        logged.ShouldNotContain(TeacherName);
    }
}
