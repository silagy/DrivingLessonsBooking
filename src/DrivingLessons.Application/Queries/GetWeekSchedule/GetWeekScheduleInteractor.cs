using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleInteractor
{
    private readonly IWeekScheduleQueries queries;
    private readonly ICurrentUser currentUser;

    public GetWeekScheduleInteractor(IWeekScheduleQueries queries, ICurrentUser currentUser)
    {
        this.queries = queries;
        this.currentUser = currentUser;
    }

    public async Task<GetWeekScheduleResponse> ExecuteAsync(Guid teacherId, DateOnly weekStart)
    {
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        var weekSchedule = await queries.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (weekSchedule is null)
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        return weekSchedule;
    }
}
