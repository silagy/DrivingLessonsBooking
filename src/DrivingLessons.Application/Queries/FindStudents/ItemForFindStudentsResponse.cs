namespace DrivingLessons.Application.Queries.FindStudents;

public class ItemForFindStudentsResponse
{
    public Guid Id { get; init; }
    public string NationalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid CarId { get; init; }
    public string CarName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
