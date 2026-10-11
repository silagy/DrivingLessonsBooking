using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeStudentCar;

public class ChangeStudentCarInteractor
{
    private readonly IStudentRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly ITeacherAndCarLocks locks;
    private readonly IUnitOfWork unitOfWork;

    public ChangeStudentCarInteractor(
        IStudentRepository repository,
        ITeacherRepository teacherRepository,
        ICarRepository carRepository,
        ITeacherAndCarLocks locks,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.carRepository = carRepository;
        this.locks = locks;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeStudentCarRequest request)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var teacherId = student.TeacherId;
        var carId = CarId.Of(request.CarId);

        await locks.LockAsync([teacherId], [carId]);

        var teacher = await teacherRepository.GetAsync(teacherId);

        if (teacher is null)
        {
            throw new TeacherNotFoundException(teacherId);
        }

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        student.ChangeCar(car);

        await unitOfWork.CommitAsync();
    }
}
