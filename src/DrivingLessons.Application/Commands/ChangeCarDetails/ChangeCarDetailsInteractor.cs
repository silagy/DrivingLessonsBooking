using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeCarDetails;

public class ChangeCarDetailsInteractor
{
    private readonly ICarRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeCarDetailsInteractor(ICarRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeCarDetailsRequest request)
    {
        var carId = CarId.Of(id);

        var car = await repository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var name = CarName.Of(request.Name);
        var type = CarType.Of(request.Type);
        car.ChangeDetails(name, type, request.Transmission);

        await unitOfWork.CommitAsync();
    }
}
