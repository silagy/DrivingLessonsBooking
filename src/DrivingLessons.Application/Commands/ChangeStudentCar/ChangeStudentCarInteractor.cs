using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeStudentCar;

public class ChangeStudentCarInteractor
{
    private readonly IStudentRepository repository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeStudentCarInteractor(
        IStudentRepository repository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeStudentCarRequest request)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var carId = CarId.Of(request.CarId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        student.ChangeCar(car);

        await unitOfWork.CommitAsync();
    }
}
