using System.Security.Claims;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class AuthorizationPoliciesTest
{
    [TestMethod]
    [DataRow(AuthClaims.AdministratorRole, AuthorizationPolicies.Administrator, true)]
    [DataRow(AuthClaims.TeacherRole, AuthorizationPolicies.Administrator, false)]
    [DataRow(AuthClaims.AdministratorRole, AuthorizationPolicies.TeacherOrAdministrator, true)]
    [DataRow(AuthClaims.TeacherRole, AuthorizationPolicies.TeacherOrAdministrator, true)]
    [DataRow("student", AuthorizationPolicies.Administrator, false)]
    [DataRow("student", AuthorizationPolicies.TeacherOrAdministrator, false)]
    public async Task Policy_Admits_Only_Its_Roles(string role, string policy, bool expected)
    {
        //given
        var authorization = Services().GetRequiredService<IAuthorizationService>();
        var user = SignedIn(role);

        //when
        var result = await authorization.AuthorizeAsync(user, policy);

        //then
        result.Succeeded.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(AuthClaims.AdministratorRole, true)]
    [DataRow(AuthClaims.TeacherRole, false)]
    public async Task Endpoints_Without_A_Policy_Are_Administrator_Only(string role, bool expected)
    {
        //given
        var services = Services();
        var options = services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var user = SignedIn(role);

        //when
        var fallback = await authorization.AuthorizeAsync(user, options.FallbackPolicy!);
        var byDefault = await authorization.AuthorizeAsync(user, options.DefaultPolicy);

        //then
        fallback.Succeeded.ShouldBe(expected);
        byDefault.Succeeded.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Anonymous_Caller_Passes_No_Policy()
    {
        //given
        var authorization = Services().GetRequiredService<IAuthorizationService>();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        //when
        var result = await authorization.AuthorizeAsync(anonymous, AuthorizationPolicies.TeacherOrAdministrator);

        //then
        result.Succeeded.ShouldBeFalse();
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal SignedIn(string role)
    {
        Claim[] claims = [new(AuthClaims.Role, role)];
        var identity = new ClaimsIdentity(claims, "Bearer", "sub", AuthClaims.Role);

        return new ClaimsPrincipal(identity);
    }
}
