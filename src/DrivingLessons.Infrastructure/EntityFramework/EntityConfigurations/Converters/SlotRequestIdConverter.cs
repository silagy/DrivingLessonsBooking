using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SlotRequestIdConverter : ValueConverter<SlotRequestId, Guid>
{
    public SlotRequestIdConverter()
        : base(
            id => id.Value,
            value => SlotRequestId.Of(value))
    {
    }
}
