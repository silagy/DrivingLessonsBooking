using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReactivateStudent;

public class ReactivateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly ITeacherAndCarLocks locks;
    private readonly IUnitOfWork unitOfWork;

    public ReactivateStudentInteractor(
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

    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        await locks.LockAsync([student.TeacherId], [student.CarId]);

        var teacher = await teacherRepository.GetIncludingDeletedAsync(student.TeacherId)
                      ?? throw new TeacherNotFoundException(student.TeacherId);

        var car = await carRepository.GetIncludingDeletedAsync(student.CarId)
                  ?? throw new CarNotFoundException(student.CarId);

        student.Reactivate(teacher, car);

        await unitOfWork.CommitAsync();
    }
}
