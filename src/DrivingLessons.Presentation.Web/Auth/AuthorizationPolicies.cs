using DrivingLessons.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace DrivingLessons.Presentation.Web.Auth;

public static class AuthorizationPolicies
{
    public const string Administrator = "Administrator";
    public const string TeacherOrAdministrator = "TeacherOrAdministrator";

    public static void Configure(AuthorizationOptions options)
    {
        var administrator = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(AuthClaims.AdministratorRole)
            .Build();
        var teacherOrAdministrator = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(AuthClaims.AdministratorRole, AuthClaims.TeacherRole)
            .Build();

        options.DefaultPolicy = administrator;
        options.FallbackPolicy = administrator;
        options.AddPolicy(Administrator, administrator);
        options.AddPolicy(TeacherOrAdministrator, teacherOrAdministrator);
    }
}
