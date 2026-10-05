using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.MarkSlotUnavailable;

public class MarkSlotUnavailableInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUser currentUser;

    public MarkSlotUnavailableInteractor(
        IWeekScheduleRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
    }

    public async Task ExecuteAsync(Guid id, Guid slotId)
    {
        var weekScheduleId = WeekScheduleId.Of(id);

        var weekSchedule = await repository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        if (!currentUser.MayReach(weekSchedule.TeacherId))
        {
            throw new WeekScheduleNotFoundException(weekScheduleId);
        }

        var resolvedSlotId = SlotId.Of(slotId);

        var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == resolvedSlotId)
                   ?? throw new SlotNotFoundException(weekScheduleId, resolvedSlotId);

        weekSchedule.MarkSlotUnavailable(slot);

        await unitOfWork.CommitAsync();
    }
}
