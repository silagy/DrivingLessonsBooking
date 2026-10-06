using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReactivateStudent;

public class ReactivateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ReactivateStudentInteractor(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        student.Reactivate();

        await unitOfWork.CommitAsync();
    }
}
