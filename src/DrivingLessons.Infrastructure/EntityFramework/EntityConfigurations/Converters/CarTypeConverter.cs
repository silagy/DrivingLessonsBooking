using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class CarTypeConverter : ValueConverter<CarType, string>
{
    public CarTypeConverter()
        : base(
            type => type.Value,
            value => CarType.Of(value))
    {
    }
}
