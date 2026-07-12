using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateWeekSchedule;

public class CreateWeekScheduleInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateWeekScheduleInteractor(
        IWeekScheduleRepository repository,
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateWeekScheduleResponse> ExecuteAsync(CreateWeekScheduleRequest request)
    {
        var teacherId = TeacherId.Of(request.TeacherId);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var weekStart = WeekStart.Of(request.WeekStart);

        var existingWeekSchedule = await repository.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (existingWeekSchedule is not null)
        {
            throw new WeekScheduleAlreadyExistsException(teacherId, weekStart);
        }

        var weekSchedule = WeekSchedule.Create(teacher, weekStart);

        repository.Add(weekSchedule);

        await unitOfWork.CommitAsync();

        return new CreateWeekScheduleResponse(weekSchedule.Id.Value);
    }
}
