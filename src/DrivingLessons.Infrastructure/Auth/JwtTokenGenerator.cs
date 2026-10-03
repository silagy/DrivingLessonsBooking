using System.Text;
using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public IssuedToken Generate(User user)
    {
        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(jwt.ExpiryHours);
        var claims = ClaimsOf(user);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return new IssuedToken(token, expiresAt);
    }

    private static Dictionary<string, object> ClaimsOf(User user)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.Value.ToString(),
            [JwtRegisteredClaimNames.Email] = user.SignInEmail.Value,
            [AuthClaims.Role] = RoleClaimOf(user.Role),
            [AuthClaims.SecurityStamp] = user.SecurityStamp.Value
        };

        var teacherId = user.TeacherId;

        if (teacherId is not null)
        {
            claims[AuthClaims.TeacherId] = teacherId.Value.ToString();
        }

        return claims;
    }

    private static string RoleClaimOf(Role role)
    {
        return role switch
        {
            Role.Administrator => AuthClaims.AdministratorRole,
            Role.Teacher => AuthClaims.TeacherRole,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
}
