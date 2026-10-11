using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeUserRole;

public class ChangeUserRoleInteractor
{
    private const int LastAdministratorCount = 1;

    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly ICurrentUser currentUser;
    private readonly ITeacherAndCarLocks locks;
    private readonly IUnitOfWork unitOfWork;

    public ChangeUserRoleInteractor(
        IUserRepository repository,
        IUserQueries queries,
        ICurrentUser currentUser,
        ITeacherAndCarLocks locks,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.currentUser = currentUser;
        this.locks = locks;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeUserRoleRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        await LockLinkedTeacherAsync(user);

        MustNotBeCurrentUser(userId);

        await MustNotDemoteLastActiveAdministratorAsync(user, request.Role);

        user.ChangeRole(request.Role);

        await unitOfWork.CommitAsync();
    }

    private async Task LockLinkedTeacherAsync(User user)
    {
        var teacherId = user.TeacherId;

        if (teacherId is null)
        {
            return;
        }

        await locks.LockAsync([teacherId], []);
    }

    private void MustNotBeCurrentUser(UserId userId)
    {
        if (userId == currentUser.Id)
        {
            throw new UserMustNotChangeOwnRoleException();
        }
    }

    private async Task MustNotDemoteLastActiveAdministratorAsync(User user, Role role)
    {
        if (user.Role is not Role.Administrator
            || role is not Role.Teacher
            || user.IsDeleted)
        {
            return;
        }

        var activeAdministrators = await queries.CountActiveAdministratorsAsync();

        if (activeAdministrators <= LastAdministratorCount)
        {
            throw new UserMustNotDemoteLastActiveAdministratorException();
        }
    }
}
