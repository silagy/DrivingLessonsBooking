using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DrivingLessons.Presentation.Web.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId Id => SignedInUserId();

    private UserId SignedInUserId()
    {
        var subject = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(subject, out var id))
        {
            throw new InvalidOperationException("The request has no signed-in User.");
        }

        return UserId.Of(id);
    }
}
