using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class WeekScheduleIdConverter : ValueConverter<WeekScheduleId, Guid>
{
    public WeekScheduleIdConverter()
        : base(
            id => id.Value,
            value => WeekScheduleId.Of(value))
    {
    }
}
