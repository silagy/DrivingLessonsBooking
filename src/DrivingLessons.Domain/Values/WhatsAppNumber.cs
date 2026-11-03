namespace DrivingLessons.Domain.Values;

public record WhatsAppNumber
{
    public string Value { get; }

    private WhatsAppNumber(string value)
    {
        Value = value;
    }

    internal static WhatsAppNumber Of(string digits)
    {
        return new WhatsAppNumber(digits);
    }
}
