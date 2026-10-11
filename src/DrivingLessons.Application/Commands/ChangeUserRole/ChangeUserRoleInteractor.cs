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
    private readonly IUnitOfWork unitOfWork;

    public ChangeUserRoleInteractor(
        IUserRepository repository,
        IUserQueries queries,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.currentUser = currentUser;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeUserRoleRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        MustNotBeCurrentUser(userId);

        await MustNotDemoteLastActiveAdministratorAsync(user, request.Role);

        user.ChangeRole(request.Role);

        await unitOfWork.CommitAsync();
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
