using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class PhoneNumberConverter : ValueConverter<PhoneNumber, string>
{
    public PhoneNumberConverter()
        : base(
            phone => phone.Value,
            value => PhoneNumber.Of(value))
    {
    }
}
