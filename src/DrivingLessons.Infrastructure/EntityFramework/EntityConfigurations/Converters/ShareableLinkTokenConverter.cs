using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class ShareableLinkTokenConverter : ValueConverter<ShareableLinkToken, string>
{
    public ShareableLinkTokenConverter()
        : base(
            token => token.Value,
            value => ShareableLinkToken.Of(value))
    {
    }
}
