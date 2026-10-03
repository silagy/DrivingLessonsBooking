using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class UserNameConverter : ValueConverter<UserName, string>
{
    public UserNameConverter()
        : base(
            name => name.Value,
            value => UserName.Of(value))
    {
    }
}
