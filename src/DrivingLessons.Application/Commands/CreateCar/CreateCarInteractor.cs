using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateCar;

public class CreateCarInteractor
{
    private readonly ICarRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public CreateCarInteractor(ICarRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateCarResponse> ExecuteAsync(CreateCarRequest request)
    {
        var name = CarName.Of(request.Name);
        var type = CarType.Of(request.Type);
        var car = Car.Create(name, type, request.Transmission);

        repository.Add(car);

        await unitOfWork.CommitAsync();

        return new CreateCarResponse(car.Id.Value);
    }
}
