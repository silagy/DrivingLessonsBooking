using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeleteCar;

public class DeleteCarInteractor
{
    private readonly ICarRepository repository;
    private readonly IStudentRepository studentRepository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCarInteractor(ICarRepository repository, IStudentRepository studentRepository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.studentRepository = studentRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var carId = CarId.Of(id);

        var car = await repository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var students = await studentRepository.FindByCarAsync(carId);

        car.Delete(students);

        await unitOfWork.CommitAsync();
    }
}
