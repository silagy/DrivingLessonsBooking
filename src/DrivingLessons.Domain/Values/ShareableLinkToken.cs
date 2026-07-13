using System.Security.Cryptography;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record ShareableLinkToken
{
    private const int TokenByteLength = 32;

    public string Value { get; }

    private ShareableLinkToken(string value)
    {
        Value = value;
    }

    public static ShareableLinkToken New()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var base64 = Convert.ToBase64String(bytes);
        var token = base64
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", string.Empty);

        return new ShareableLinkToken(token);
    }

    public static ShareableLinkToken Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ShareableLinkTokenMustNotBeEmptyException();
        }

        return new ShareableLinkToken(value.Trim());
    }
}
