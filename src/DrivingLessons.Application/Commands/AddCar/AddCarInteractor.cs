using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.AddCar;

public class AddCarInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public AddCarInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<AddCarResponse> ExecuteAsync(Guid id, AddCarRequest request)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var name = CarName.Of(request.Name);
        var type = CarType.Of(request.Type);
        var car = teacher.AddCar(name, type, request.Transmission);

        await unitOfWork.CommitAsync();

        return new AddCarResponse(car.Id.Value);
    }
}
