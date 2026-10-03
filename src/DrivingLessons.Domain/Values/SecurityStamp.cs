using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SecurityStamp
{
    private const string CompactGuidFormat = "N";

    public string Value { get; }

    private SecurityStamp(string value)
    {
        Value = value;
    }

    public static SecurityStamp New()
    {
        var value = Guid.NewGuid().ToString(CompactGuidFormat);

        return new SecurityStamp(value);
    }

    public static SecurityStamp Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SecurityStampMustNotBeEmptyException();
        }

        return new SecurityStamp(value);
    }
}
