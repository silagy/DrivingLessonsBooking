using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter()
        : base(
            address => address.Value,
            value => Address.Of(value))
    {
    }
}
