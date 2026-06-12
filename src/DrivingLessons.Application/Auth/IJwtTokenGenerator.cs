namespace DrivingLessons.Application.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    IssuedToken Generate(Guid adminId, string email);
}
