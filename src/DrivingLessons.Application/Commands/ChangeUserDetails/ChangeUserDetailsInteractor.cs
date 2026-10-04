using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeUserDetails;

public class ChangeUserDetailsInteractor
{
    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly IUnitOfWork unitOfWork;

    public ChangeUserDetailsInteractor(
        IUserRepository repository,
        IUserQueries queries,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeUserDetailsRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        var name = UserName.Of(request.Name);
        var signInEmail = Email.Of(request.SignInEmail);

        await SignInEmailMustBeFreeAsync(user, signInEmail);

        user.ChangeDetails(name, signInEmail);

        await unitOfWork.CommitAsync();
    }

    private async Task SignInEmailMustBeFreeAsync(User user, Email signInEmail)
    {
        if (signInEmail == user.SignInEmail)
        {
            return;
        }

        var inUse = await queries.ExistsWithSignInEmailAsync(signInEmail);

        if (inUse)
        {
            throw new UserSignInEmailAlreadyInUseException();
        }
    }
}
