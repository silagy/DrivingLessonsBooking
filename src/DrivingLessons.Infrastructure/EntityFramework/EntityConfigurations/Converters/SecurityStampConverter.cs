using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SecurityStampConverter : ValueConverter<SecurityStamp, string>
{
    public SecurityStampConverter()
        : base(
            stamp => stamp.Value,
            value => SecurityStamp.Of(value))
    {
    }
}
