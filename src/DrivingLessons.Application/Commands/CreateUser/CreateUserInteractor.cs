using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateUser;

public class CreateUserInteractor
{
    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly ITeacherRepository teacherRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public CreateUserInteractor(
        IUserRepository repository,
        IUserQueries queries,
        ITeacherRepository teacherRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.teacherRepository = teacherRepository;
        this.passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResponse> ExecuteAsync(CreateUserRequest request)
    {
        var name = UserName.Of(request.Name);
        var signInEmail = Email.Of(request.SignInEmail);
        var temporaryPassword = TemporaryPassword.Of(request.TemporaryPassword);

        await SignInEmailMustBeFreeAsync(signInEmail);

        var teacher = await ResolveUnlinkedTeacherAsync(request.TeacherId);

        var passwordHash = passwordHasher.Hash(temporaryPassword.Value);
        var user = User.Create(name, signInEmail, passwordHash, request.Role, teacher);

        repository.Add(user);

        await unitOfWork.CommitAsync();

        return new CreateUserResponse(user.Id.Value);
    }

    private async Task SignInEmailMustBeFreeAsync(Email signInEmail)
    {
        var inUse = await queries.ExistsWithSignInEmailAsync(signInEmail);

        if (inUse)
        {
            throw new UserSignInEmailAlreadyInUseException();
        }
    }

    private async Task<Teacher?> ResolveUnlinkedTeacherAsync(Guid? id)
    {
        if (id is null)
        {
            return null;
        }

        var teacherId = TeacherId.Of(id.Value);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var alreadyLinked = await queries.ExistsLinkedToTeacherAsync(teacherId);

        if (alreadyLinked)
        {
            throw new TeacherAlreadyLinkedToUserException(teacherId);
        }

        return teacher;
    }
}
