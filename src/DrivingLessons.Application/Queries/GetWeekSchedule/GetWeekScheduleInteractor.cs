using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleInteractor
{
    private readonly IWeekScheduleQueries queries;

    public GetWeekScheduleInteractor(IWeekScheduleQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetWeekScheduleResponse> ExecuteAsync(Guid teacherId, DateOnly weekStart)
    {
        var weekSchedule = await queries.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (weekSchedule is null)
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        return weekSchedule;
    }
}
