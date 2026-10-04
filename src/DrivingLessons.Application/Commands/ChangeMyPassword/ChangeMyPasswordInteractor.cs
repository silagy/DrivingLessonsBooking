using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeMyPassword;

public class ChangeMyPasswordInteractor
{
    private readonly ICurrentUser currentUser;
    private readonly IUserRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtTokenGenerator tokenGenerator;
    private readonly IUnitOfWork unitOfWork;

    public ChangeMyPasswordInteractor(
        ICurrentUser currentUser,
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork)
    {
        this.currentUser = currentUser;
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.tokenGenerator = tokenGenerator;
        this.unitOfWork = unitOfWork;
    }

    public async Task<ChangeMyPasswordResponse> ExecuteAsync(ChangeMyPasswordRequest request)
    {
        var userId = currentUser.Id;

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        var newPassword = Password.Of(request.NewPassword);
        var currentPassword = request.CurrentPassword ?? string.Empty;

        MustMatchCurrentPassword(user, currentPassword);

        var passwordHash = passwordHasher.Hash(newPassword.Value);

        user.ChangePassword(passwordHash);

        await unitOfWork.CommitAsync();

        var issued = tokenGenerator.Generate(user);

        return new ChangeMyPasswordResponse(issued.AccessToken, issued.ExpiresAtUtc);
    }

    private void MustMatchCurrentPassword(User user, string currentPassword)
    {
        if (!passwordHasher.Verify(user.PasswordHash, currentPassword))
        {
            throw new UserCurrentPasswordMustBeCorrectException(user.Id);
        }
    }
}
