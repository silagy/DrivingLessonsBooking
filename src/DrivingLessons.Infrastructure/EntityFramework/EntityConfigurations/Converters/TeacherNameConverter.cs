using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherNameConverter : ValueConverter<TeacherName, string>
{
    public TeacherNameConverter()
        : base(
            name => name.Value,
            value => TeacherName.Of(value))
    {
    }
}
