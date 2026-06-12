using System.Text;
using DrivingLessons.Application.Auth;
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public IssuedToken Generate(Guid adminId, string email)
    {
        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(jwt.ExpiryHours);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = adminId.ToString(),
                [JwtRegisteredClaimNames.Email] = email,
                ["role"] = "admin"
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return new IssuedToken(token, expiresAt);
    }
}
