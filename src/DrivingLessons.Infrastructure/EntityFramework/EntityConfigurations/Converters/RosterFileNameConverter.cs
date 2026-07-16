using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class RosterFileNameConverter : ValueConverter<RosterFileName, string>
{
    public RosterFileNameConverter()
        : base(
            fileName => fileName.Value,
            value => RosterFileName.Of(value))
    {
    }
}
