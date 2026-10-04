using System.Security.Claims;
using DrivingLessons.Domain.Values;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class HttpCurrentUserTest
{
    [TestMethod]
    public void Reads_The_Signed_In_User_From_The_Subject_Claim()
    {
        //given
        var id = Guid.NewGuid();
        var accessor = AccessorWith(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()));

        //when
        var currentUserId = new HttpCurrentUser(accessor).Id;

        //then
        currentUserId.ShouldBe(UserId.Of(id));
    }

    [TestMethod]
    public void Request_Without_A_Signed_In_User_Fails_Loudly()
    {
        //given
        var accessor = AccessorWith();

        //when
        var act = () => new HttpCurrentUser(accessor).Id;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    private static HttpContextAccessor AccessorWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
