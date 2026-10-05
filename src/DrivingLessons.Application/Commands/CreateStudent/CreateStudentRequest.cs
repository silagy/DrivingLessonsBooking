namespace DrivingLessons.Application.Commands.CreateStudent;

public record CreateStudentRequest(
    string NationalId,
    string Name,
    string Phone,
    Guid TeacherId,
    Guid CarId,
    string? Address,
    DateOnly? StartDate,
    string? LicenseType);
