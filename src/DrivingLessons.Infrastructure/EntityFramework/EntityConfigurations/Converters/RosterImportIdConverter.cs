using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class RosterImportIdConverter : ValueConverter<RosterImportId, Guid>
{
    public RosterImportIdConverter()
        : base(
            id => id.Value,
            value => RosterImportId.Of(value))
    {
    }
}
