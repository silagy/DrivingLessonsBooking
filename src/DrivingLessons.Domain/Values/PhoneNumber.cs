using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record PhoneNumber
{
    private const int MinimumDigitCount = 7;

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

    private static string RemoveBidiMarks(string value)
    {
        var kept = value.Where(c => !BidiMarks.Contains(c));

        return string.Concat(kept);
    }
}
