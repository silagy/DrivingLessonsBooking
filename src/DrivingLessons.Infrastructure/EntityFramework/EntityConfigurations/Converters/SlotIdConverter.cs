using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SlotIdConverter : ValueConverter<SlotId, Guid>
{
    public SlotIdConverter()
        : base(
            id => id.Value,
            value => SlotId.Of(value))
    {
    }
}
