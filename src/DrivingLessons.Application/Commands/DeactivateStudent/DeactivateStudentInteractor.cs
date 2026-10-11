using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeactivateStudent;

public class DeactivateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeactivateStudentInteractor(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        student.Deactivate();

        await unitOfWork.CommitAsync();
    }
}
