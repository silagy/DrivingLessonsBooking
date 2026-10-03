using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IUserQueries
{
    Task<bool> ExistsWithSignInEmailAsync(Email signInEmail);

    Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId);
}
