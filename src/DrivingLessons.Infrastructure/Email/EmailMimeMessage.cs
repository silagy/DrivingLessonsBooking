using System.Net;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Options;
using MimeKit;

namespace DrivingLessons.Infrastructure.Email;

public static class EmailMimeMessage
{
    private const char LineSeparator = '\n';

    public static MimeMessage Create(EmailMessage message, EmailOptions options)
    {
        var from = new MailboxAddress(options.FromName, options.FromAddress);
        var to = MailboxAddress.Parse(message.ToEmail);

        var attachment = message.Attachment;
        var contentType = ContentType.Parse(attachment.ContentType);

        var body = new BodyBuilder
        {
            TextBody = message.Body,
            HtmlBody = RightToLeftHtml(message.Body)
        };

        body.Attachments.Add(attachment.FileName, attachment.Content, contentType);

        var mime = new MimeMessage();
        mime.From.Add(from);
        mime.To.Add(to);
        mime.Subject = message.Subject;
        mime.Body = body.ToMessageBody();

        return mime;
    }

    private static string RightToLeftHtml(string body)
    {
        var paragraphs = body
                             .Split(LineSeparator)
                             .Select(line => $"<p>{WebUtility.HtmlEncode(line)}</p>");

        return $"<div dir=\"rtl\" lang=\"he\">{string.Concat(paragraphs)}</div>";
    }
}
