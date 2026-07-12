using DrivingLessons.Application.Queries.GetWeekSchedule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.WeekSchedule;

[ApiController]
[Route("api/week-schedules")]
[Tags("Week Schedules")]
public class WeekScheduleQueryController : ControllerBase
{
    [HttpGet("by-teacher-and-week")]
    [EndpointSummary("Gets the week schedule for a teacher and week")]
    [ProducesResponseType(typeof(GetWeekScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<GetWeekScheduleResponse> GetByTeacherAndWeek(
        [FromServices] GetWeekScheduleInteractor interactor,
        [FromQuery] Guid teacherId,
        [FromQuery] DateOnly week)
    {
        return await interactor.ExecuteAsync(teacherId, week);
    }
}
