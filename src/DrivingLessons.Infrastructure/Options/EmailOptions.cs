using System.ComponentModel.DataAnnotations;
using MailKit.Security;
using MimeKit;

namespace DrivingLessons.Infrastructure.Options;

public sealed class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    private const int DefaultPort = 587;

    public bool Enabled { get; init; }

    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = DefaultPort;

    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.StartTls;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            yield return new ValidationResult("Email host is required when email is enabled.", [nameof(Host)]);
        }

        if (!IsMailboxAddress(FromAddress))
        {
            yield return new ValidationResult(
                "A valid from address is required when email is enabled.",
                [nameof(FromAddress)]);
        }

        var hasUsername = !string.IsNullOrEmpty(Username);

        if (hasUsername
            && string.IsNullOrEmpty(Password))
        {
            yield return new ValidationResult("An email password is required with a username.", [nameof(Password)]);
        }
    }

    private static bool IsMailboxAddress(string address)
    {
        var emailAddress = new EmailAddressAttribute();

        if (string.IsNullOrWhiteSpace(address)
            || !emailAddress.IsValid(address))
        {
            return false;
        }

        var parsed = MailboxAddress.TryParse(address, out var mailbox);

        return parsed && mailbox.Address == address;
    }
}
