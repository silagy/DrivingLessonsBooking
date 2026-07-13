using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class EmailConverter : ValueConverter<DrivingLessons.Domain.Values.Email, string>
{
    public EmailConverter()
        : base(
            email => email.Value,
            value => DrivingLessons.Domain.Values.Email.Of(value))
    {
    }
}
