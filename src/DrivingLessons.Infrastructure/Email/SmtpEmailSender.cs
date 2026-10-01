using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrivingLessons.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private const int TimeoutMilliseconds = 30_000;

    private readonly EmailOptions options;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message)
    {
        if (!options.Enabled)
        {
            var attachmentBytes = message.Attachment.Content.Length;
            logger.LogInformation("Email disabled. Skipped send of [{AttachmentBytes}] bytes.", attachmentBytes);

            return;
        }

        var mime = EmailMimeMessage.Create(message, options);

        using var client = new SmtpClient();
        client.Timeout = TimeoutMilliseconds;

        await client.ConnectAsync(options.Host, options.Port, options.Security);

        if (!string.IsNullOrEmpty(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password);
        }

        const bool quit = true;
        await client.SendAsync(mime);
        await client.DisconnectAsync(quit);
    }
}
