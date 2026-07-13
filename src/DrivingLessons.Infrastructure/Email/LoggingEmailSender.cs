using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrivingLessons.Infrastructure.Email;

public class LoggingEmailSender : IEmailSender
{
    private readonly EmailOptions options;
    private readonly ILogger<LoggingEmailSender> logger;

    public LoggingEmailSender(IOptions<EmailOptions> options, ILogger<LoggingEmailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public Task SendAsync(EmailMessage message)
    {
        if (!options.Enabled)
        {
            logger.LogInformation(
                "Email disabled. Skipped send to [{Recipient}] subject [{Subject}] attachment [{AttachmentBytes}] bytes.",
                message.ToEmail,
                message.Subject,
                message.Attachment.Content.Length);

            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
