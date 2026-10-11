using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeleteTeacher;

public class DeleteTeacherInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUserQueries userQueries;
    private readonly IStudentRepository studentRepository;
    private readonly ITeacherAndCarLocks locks;
    private readonly IUnitOfWork unitOfWork;

    public DeleteTeacherInteractor(
        ITeacherRepository repository,
        IUserQueries userQueries,
        IStudentRepository studentRepository,
        ITeacherAndCarLocks locks,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.userQueries = userQueries;
        this.studentRepository = studentRepository;
        this.locks = locks;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var teacherId = TeacherId.Of(id);

        await locks.LockAsync([teacherId], []);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var hasActiveUser = await userQueries.ActiveExistsLinkedToTeacherAsync(teacherId);

        if (hasActiveUser)
        {
            throw new TeacherMustNotHaveActiveUserException(teacherId);
        }

        var students = await studentRepository.FindActiveByTeacherAsync(teacherId);

        teacher.Delete(students);

        await unitOfWork.CommitAsync();
    }
}
