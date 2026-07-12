using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class CarIdConverter : ValueConverter<CarId, Guid>
{
    public CarIdConverter()
        : base(
            id => id.Value,
            value => CarId.Of(value))
    {
    }
}
