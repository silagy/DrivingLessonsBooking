using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.SetUserTemporaryPassword;

public class SetUserTemporaryPasswordInteractor
{
    private readonly IUserRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public SetUserTemporaryPasswordInteractor(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, SetUserTemporaryPasswordRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        var temporaryPassword = TemporaryPassword.Of(request.TemporaryPassword);
        var passwordHash = passwordHasher.Hash(temporaryPassword.Value);

        user.SetTemporaryPassword(passwordHash);

        await unitOfWork.CommitAsync();
    }
}
