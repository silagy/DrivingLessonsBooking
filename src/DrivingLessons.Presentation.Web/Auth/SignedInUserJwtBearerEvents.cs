using DrivingLessons.Application.Auth;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DrivingLessons.Presentation.Web.Auth;

public sealed class SignedInUserJwtBearerEvents(CheckSignedInUserInteractor checkSignedInUser) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userIdClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var securityStampClaim = context.Principal?.FindFirst(AuthClaims.SecurityStamp)?.Value;

        var signedIn = await checkSignedInUser.ExecuteAsync(userIdClaim, securityStampClaim);

        if (!signedIn)
        {
            context.Fail("The User is deleted or their sign-in was revoked.");
        }
    }
}
