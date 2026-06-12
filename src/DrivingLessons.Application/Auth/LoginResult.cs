namespace DrivingLessons.Application.Auth;

public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
