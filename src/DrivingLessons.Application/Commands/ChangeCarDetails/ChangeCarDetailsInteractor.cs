using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeCarDetails;

public class ChangeCarDetailsInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeCarDetailsInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid carId, ChangeCarDetailsRequest request)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var resolvedCarId = CarId.Of(carId);
        var car = teacher.Cars.FirstOrDefault(x => x.Id == resolvedCarId)
                  ?? throw new CarNotFoundException(resolvedCarId);

        var name = CarName.Of(request.Name);
        var type = CarType.Of(request.Type);
        teacher.ChangeCarDetails(car, name, type, request.Transmission);

        await unitOfWork.CommitAsync();
    }
}
