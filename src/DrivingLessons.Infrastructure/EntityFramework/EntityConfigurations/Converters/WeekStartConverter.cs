using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class WeekStartConverter : ValueConverter<WeekStart, DateOnly>
{
    public WeekStartConverter()
        : base(
            weekStart => weekStart.Value,
            value => WeekStart.Of(value))
    {
    }
}
