using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SubmissionIdConverter : ValueConverter<SubmissionId, Guid>
{
    public SubmissionIdConverter()
        : base(
            id => id.Value,
            value => SubmissionId.Of(value))
    {
    }
}
