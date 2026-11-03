using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record PhoneNumber
{
    private const int MinimumDigitCount = 7;
    private const char PlusSign = '+';
    private const string LocalMobilePrefix = "05";
    private const string InternationalMobilePrefix = "9725";
    private const string DialOutMobilePrefix = "009725";
    private const int LocalMobileLength = 10;
    private const int InternationalMobileLength = 12;
    private const int DialOutMobileLength = 14;

    private static readonly char[] Separators =
    [
        ' ',
        '-',
        '(',
        ')',
        '.'
    ];

    private static readonly char[] BidiMarks =
    [
        '\u200E',
        '\u200F',
        '\u202A',
        '\u202B',
        '\u202C',
        '\u202D',
        '\u202E',
        '\u2066',
        '\u2067',
        '\u2068',
        '\u2069'
    ];

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Of(string value)
    {
        var withoutMarks = RemoveBidiMarks(value);
        var normalized = withoutMarks.Replace('\u00A0', ' ').Trim();
        var digitCount = normalized.Count(char.IsAsciiDigit);

        if (digitCount < MinimumDigitCount)
        {
            throw new PhoneNumberMustBeValidException();
        }

        return new PhoneNumber(normalized);
    }

    public WhatsAppNumber? ToWhatsAppNumber()
    {
        var hasPlusSign = Value.StartsWith(PlusSign);
        var afterPlusSign = hasPlusSign
                                ? Value[1..]
                                : Value;

        var hasOnlyDigitsAndSeparators = afterPlusSign.All(IsDigitOrSeparator);

        if (!hasOnlyDigitsAndSeparators)
        {
            return null;
        }

        var digitCharacters = afterPlusSign.Where(char.IsAsciiDigit);
        var digits = string.Concat(digitCharacters);

        var isInternationalMobile = digits.Length == InternationalMobileLength
                                    && digits.StartsWith(InternationalMobilePrefix, StringComparison.Ordinal);

        if (isInternationalMobile)
        {
            return WhatsAppNumber.Of(digits);
        }

        if (hasPlusSign)
        {
            return null;
        }

        var isLocalMobile = digits.Length == LocalMobileLength
                            && digits.StartsWith(LocalMobilePrefix, StringComparison.Ordinal);

        if (isLocalMobile)
        {
            var subscriberDigits = digits[LocalMobilePrefix.Length..];
            var internationalDigits = InternationalMobilePrefix + subscriberDigits;

            return WhatsAppNumber.Of(internationalDigits);
        }

        var isDialOutMobile = digits.Length == DialOutMobileLength
                              && digits.StartsWith(DialOutMobilePrefix, StringComparison.Ordinal);

        if (isDialOutMobile)
        {
            var internationalDigits = digits[^InternationalMobileLength..];

            return WhatsAppNumber.Of(internationalDigits);
        }

        return null;
    }

    private static string RemoveBidiMarks(string value)
    {
        var kept = value.Where(c => !BidiMarks.Contains(c));

        return string.Concat(kept);
    }

    private static bool IsDigitOrSeparator(char character)
    {
        return char.IsAsciiDigit(character) || Separators.Contains(character);
    }
}
