using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DrivingLessons.Presentation.Web.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId Id => SignedInUserId();

    public Role Role => SignedInRole();

    public TeacherId? TeacherId => LinkedTeacherId();

    private UserId SignedInUserId()
    {
        var subject = ClaimValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(subject, out var id))
        {
            throw new InvalidOperationException("The request has no signed-in User.");
        }

        return UserId.Of(id);
    }

    private Role SignedInRole()
    {
        var role = ClaimValue(AuthClaims.Role);

        return role switch
        {
            AuthClaims.AdministratorRole => Role.Administrator,
            AuthClaims.TeacherRole => Role.Teacher,
            _ => throw new InvalidOperationException("The signed-in User has no known Role.")
        };
    }

    private TeacherId? LinkedTeacherId()
    {
        var teacherId = ClaimValue(AuthClaims.TeacherId);

        if (teacherId is null)
        {
            return UnlinkedTeacherId();
        }

        if (!Guid.TryParse(teacherId, out var id) || id == Guid.Empty)
        {
            throw new InvalidOperationException("The signed-in User's linked Teacher is malformed.");
        }

        return TeacherId.Of(id);
    }

    private TeacherId? UnlinkedTeacherId()
    {
        if (Role == Role.Teacher)
        {
            throw new InvalidOperationException("The signed-in Teacher-role User has no linked Teacher.");
        }

        return null;
    }

    private string? ClaimValue(string type)
    {
        var user = httpContextAccessor.HttpContext?.User;

        return user?.FindFirst(type)?.Value;
    }
}
