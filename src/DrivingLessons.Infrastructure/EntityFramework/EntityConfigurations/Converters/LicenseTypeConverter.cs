using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class LicenseTypeConverter : ValueConverter<LicenseType, string>
{
    public LicenseTypeConverter()
        : base(
            licenseType => licenseType.Value,
            value => LicenseType.Of(value))
    {
    }
}
