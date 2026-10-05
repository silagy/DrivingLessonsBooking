using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetStudent;

public class GetStudentResponse
{
    public Guid Id { get; init; }
    public string NationalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid CarId { get; init; }
    public string CarName { get; init; } = string.Empty;
    public Transmission CarTransmission { get; init; }
    public string? Address { get; init; }
    public DateOnly? StartDate { get; init; }
    public string? LicenseType { get; init; }
    public bool IsActive { get; init; }
}
