using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public class CheckSignedInUserInteractor(IUserRepository users)
{
    public async Task<bool> ExecuteAsync(string? userIdClaim, string? securityStampClaim)
    {
        if (!Guid.TryParse(userIdClaim, out var id)
            || id == Guid.Empty
            || string.IsNullOrWhiteSpace(securityStampClaim))
        {
            return false;
        }

        var userId = UserId.Of(id);
        var user = await users.GetAsync(userId);

        if (user is null
            || user.IsDeleted)
        {
            return false;
        }

        var securityStamp = SecurityStamp.Of(securityStampClaim);

        return user.SecurityStamp == securityStamp;
    }
}
