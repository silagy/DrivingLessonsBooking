using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class PublicationIdConverter : ValueConverter<PublicationId, Guid>
{
    public PublicationIdConverter()
        : base(
            id => id.Value,
            value => PublicationId.Of(value))
    {
    }
}
