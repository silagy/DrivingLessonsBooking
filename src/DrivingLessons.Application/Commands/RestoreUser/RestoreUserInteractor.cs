using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.RestoreUser;

public class RestoreUserInteractor
{
    private readonly IUserRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IUnitOfWork unitOfWork;

    public RestoreUserInteractor(
        IUserRepository repository,
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        await LinkedTeacherMustNotBeDeletedAsync(user);

        user.Restore();

        await unitOfWork.CommitAsync();
    }

    private async Task LinkedTeacherMustNotBeDeletedAsync(User user)
    {
        var teacherId = user.TeacherId;

        if (teacherId is null)
        {
            return;
        }

        var teacher = await teacherRepository.GetAsync(teacherId);

        if (teacher is null)
        {
            throw new UserLinkedTeacherMustNotBeDeletedException(user.Id);
        }
    }
}
