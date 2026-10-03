using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.SeedFirstAdministrator;

public class SeedFirstAdministratorInteractor
{
    private readonly IUserRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public SeedFirstAdministratorInteractor(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(SeedFirstAdministratorRequest request)
    {
        var usersExist = await repository.AnyExistAsync();

        if (usersExist)
        {
            return;
        }

        var name = UserName.Of(request.Name);
        var signInEmail = Email.Of(request.Email);
        var passwordHash = passwordHasher.Hash(request.Password);
        var administrator = User.Create(name, signInEmail, passwordHash, Role.Administrator, teacher: null);

        repository.Add(administrator);

        await unitOfWork.CommitAsync();
    }
}
