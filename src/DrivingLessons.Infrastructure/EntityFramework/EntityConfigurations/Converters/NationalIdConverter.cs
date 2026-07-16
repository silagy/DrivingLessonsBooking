using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class NationalIdConverter : ValueConverter<NationalId, string>
{
    public NationalIdConverter()
        : base(
            nationalId => nationalId.Value,
            value => NationalId.Of(value))
    {
    }
}
