using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class CarNameConverter : ValueConverter<CarName, string>
{
    public CarNameConverter()
        : base(
            name => name.Value,
            value => CarName.Of(value))
    {
    }
}
