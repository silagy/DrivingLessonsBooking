namespace DrivingLessons.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}

public record EmailMessage(string ToEmail, string Subject, string Body, ExcelFile Attachment);
