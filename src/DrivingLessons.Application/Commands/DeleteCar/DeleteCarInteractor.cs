using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeleteCar;

public class DeleteCarInteractor
{
    private readonly ICarRepository repository;
    private readonly IStudentRepository studentRepository;
    private readonly ITeacherAndCarLocks locks;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCarInteractor(
        ICarRepository repository,
        IStudentRepository studentRepository,
        ITeacherAndCarLocks locks,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.studentRepository = studentRepository;
        this.locks = locks;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var carId = CarId.Of(id);

        await locks.LockAsync([], [carId]);

        var car = await repository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var students = await studentRepository.FindActiveByCarAsync(carId);

        car.Delete(students);

        await unitOfWork.CommitAsync();
    }
}
