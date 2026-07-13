using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherExcelVersionIdConverter : ValueConverter<TeacherExcelVersionId, Guid>
{
    public TeacherExcelVersionIdConverter()
        : base(
            id => id.Value,
            value => TeacherExcelVersionId.Of(value))
    {
    }
}
