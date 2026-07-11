using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeTeacherDetails;

public class ChangeTeacherDetailsInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeTeacherDetailsInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeTeacherDetailsRequest request)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var name = TeacherName.Of(request.Name);
        var contactEmail = Email.Of(request.ContactEmail);
        teacher.ChangeDetails(name, contactEmail);

        await unitOfWork.CommitAsync();
    }
}
