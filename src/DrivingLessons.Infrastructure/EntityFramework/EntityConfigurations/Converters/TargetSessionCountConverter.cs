using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TargetSessionCountConverter : ValueConverter<TargetSessionCount, int>
{
    public TargetSessionCountConverter()
        : base(
            targetCount => targetCount.Value,
            value => TargetSessionCount.Of(value))
    {
    }
}
