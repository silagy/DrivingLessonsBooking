using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetUser;

public class GetUserResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SignInEmail { get; init; } = string.Empty;
    public Role Role { get; init; }
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
    public bool IsDeleted { get; init; }
}
