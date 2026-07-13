using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeleteCar;

public class DeleteCarInteractor
{
    private readonly ICarRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCarInteractor(ICarRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var carId = CarId.Of(id);

        var car = await repository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        car.Delete();

        await unitOfWork.CommitAsync();
    }
}
