using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record NationalId
{
    private const int MaxLength = 9;

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

    private NationalId(string value)
    {
        Value = value;
    }

    public static NationalId Of(string value)
    {
        var withoutMarks = RemoveBidiMarks(value);
        var normalized = withoutMarks.Replace('\u00A0', ' ').Trim();

        if (normalized.Length == 0 || !normalized.All(char.IsAsciiDigit))
        {
            throw new NationalIdMustBeDigitsException();
        }

        if (normalized.Length > MaxLength)
        {
            throw new NationalIdMustBeAtMostNineDigitsException();
        }

        var padded = normalized.PadLeft(MaxLength, '0');

        if (!HasValidCheckDigit(padded))
        {
            throw new NationalIdMustHaveValidCheckDigitException();
        }

        return new NationalId(padded);
    }

    private static string RemoveBidiMarks(string value)
    {
        var kept = value.Where(c => !BidiMarks.Contains(c));

        return string.Concat(kept);
    }

    private static bool HasValidCheckDigit(string digits)
    {
        var sum = 0;

        for (var i = 0; i < MaxLength; i++)
        {
            var weight = i % 2 == 0
                ? 1
                : 2;
            var product = (digits[i] - '0') * weight;
            var reduced = product > 9
                ? product - 9
                : product;
            sum += reduced;
        }

        return sum % 10 == 0;
    }
}
