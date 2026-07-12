using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateTeacher;

public class CreateTeacherInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public CreateTeacherInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateTeacherResponse> ExecuteAsync(CreateTeacherRequest request)
    {
        var name = TeacherName.Of(request.Name);
        var contactEmail = Email.Of(request.ContactEmail);
        var teacher = Teacher.Create(name, contactEmail);

        repository.Add(teacher);

        await unitOfWork.CommitAsync();

        return new CreateTeacherResponse(teacher.Id.Value);
    }
}
