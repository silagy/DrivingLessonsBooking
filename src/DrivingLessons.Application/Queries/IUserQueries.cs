using DrivingLessons.Application.Queries.FindUsers;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IUserQueries
{
    Task<IReadOnlyCollection<ItemForFindUsersResponse>> FindAsync();

    Task<GetUserResponse?> GetAsync(Guid id);

    Task<bool> ExistsWithSignInEmailAsync(Email signInEmail);

    Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId);

    Task<bool> ActiveExistsLinkedToTeacherAsync(TeacherId teacherId);
}
