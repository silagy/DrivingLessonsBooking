using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherAssignmentIdConverter : ValueConverter<TeacherAssignmentId, Guid>
{
    public TeacherAssignmentIdConverter()
        : base(
            id => id.Value,
            value => TeacherAssignmentId.Of(value))
    {
    }
}
