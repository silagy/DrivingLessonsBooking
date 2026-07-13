using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.AssignCarToTeacher;

public class AssignCarToTeacherInteractor
{
    private readonly ICarRepository carRepository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IUnitOfWork unitOfWork;

    public AssignCarToTeacherInteractor(
        ICarRepository carRepository,
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        this.carRepository = carRepository;
        this.teacherRepository = teacherRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid teacherId)
    {
        var carId = CarId.Of(id);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var resolvedTeacherId = TeacherId.Of(teacherId);

        var teacher = await teacherRepository.GetAsync(resolvedTeacherId)
                      ?? throw new TeacherNotFoundException(resolvedTeacherId);

        car.AssignTeacher(teacher);

        await unitOfWork.CommitAsync();
    }
}
