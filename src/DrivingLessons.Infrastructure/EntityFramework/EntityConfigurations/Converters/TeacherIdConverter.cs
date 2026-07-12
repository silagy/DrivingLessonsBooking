using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherIdConverter : ValueConverter<TeacherId, Guid>
{
    public TeacherIdConverter()
        : base(
            id => id.Value,
            value => TeacherId.Of(value))
    {
    }
}
