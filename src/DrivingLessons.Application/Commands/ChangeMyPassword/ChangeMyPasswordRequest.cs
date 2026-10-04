namespace DrivingLessons.Application.Commands.ChangeMyPassword;

public record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword);
