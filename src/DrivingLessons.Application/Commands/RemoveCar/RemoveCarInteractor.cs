using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.RemoveCar;

public class RemoveCarInteractor
{
    private readonly ITeacherRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveCarInteractor(ITeacherRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid carId)
    {
        var teacherId = TeacherId.Of(id);

        var teacher = await repository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var resolvedCarId = CarId.Of(carId);
        var car = teacher.Cars.FirstOrDefault(x => x.Id == resolvedCarId)
                  ?? throw new CarNotFoundException(resolvedCarId);

        teacher.RemoveCar(car);

        await unitOfWork.CommitAsync();
    }
}
