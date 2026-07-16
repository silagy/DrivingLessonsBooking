using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class StudentNameConverter : ValueConverter<StudentName, string>
{
    public StudentNameConverter()
        : base(
            name => name.Value,
            value => StudentName.Of(value))
    {
    }
}
