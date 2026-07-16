using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class LessonsStartDateConverter : ValueConverter<LessonsStartDate, DateOnly>
{
    public LessonsStartDateConverter()
        : base(
            startDate => startDate.Value,
            value => LessonsStartDate.Of(value))
    {
    }
}
