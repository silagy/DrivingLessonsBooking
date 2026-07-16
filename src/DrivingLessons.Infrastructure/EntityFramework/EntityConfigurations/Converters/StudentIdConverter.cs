using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class StudentIdConverter : ValueConverter<StudentId, Guid>
{
    public StudentIdConverter()
        : base(
            id => id.Value,
            value => StudentId.Of(value))
    {
    }
}
