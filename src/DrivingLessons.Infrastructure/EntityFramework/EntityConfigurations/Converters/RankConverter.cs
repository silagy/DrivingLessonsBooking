using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class RankConverter : ValueConverter<Rank, int>
{
    public RankConverter()
        : base(
            rank => rank.Value,
            value => Rank.Of(value))
    {
    }
}
