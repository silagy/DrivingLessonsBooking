namespace DrivingLessons.Application.Commands.ChangeStudentDetails;

public record ChangeStudentDetailsRequest(
    string NationalId,
    string Name,
    string Phone,
    string? Address,
    DateOnly? StartDate,
    string? LicenseType);
