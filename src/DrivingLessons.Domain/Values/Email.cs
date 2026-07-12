using System.Net.Mail;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new EmailMustBeValidException();
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out var address)
            || address.Address != normalized)
        {
            throw new EmailMustBeValidException();
        }

        return new Email(normalized);
    }
}
