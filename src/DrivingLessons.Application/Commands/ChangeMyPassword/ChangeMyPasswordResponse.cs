namespace DrivingLessons.Application.Commands.ChangeMyPassword;

public record ChangeMyPasswordResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
