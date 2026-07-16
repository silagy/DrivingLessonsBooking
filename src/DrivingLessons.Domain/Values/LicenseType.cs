using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record LicenseType
{
    public string Value { get; }

    private LicenseType(string value)
    {
        Value = value;
    }

    public static LicenseType Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LicenseTypeMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new LicenseType(normalized);
    }
}
