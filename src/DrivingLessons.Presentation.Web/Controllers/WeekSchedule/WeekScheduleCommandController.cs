using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateWeekSchedule;
using DrivingLessons.Application.Commands.MarkSlotUnavailable;
using DrivingLessons.Application.Commands.MarkSlotAvailable;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.WeekSchedule;

[ApiController]
[Route("api/week-schedules")]
[Tags("Week Schedules")]
public class WeekScheduleCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Creates a week schedule for a teacher and week with all slots open")]
    [ProducesResponseType(typeof(CreateWeekScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromServices] CreateWeekScheduleInteractor interactor,
        [FromBody] [Required] CreateWeekScheduleRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPost("{id:guid}/slots/{slotId:guid}/mark-unavailable")]
    [EndpointSummary("Marks an open slot unavailable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkSlotUnavailable(
        [FromServices] MarkSlotUnavailableInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid slotId)
    {
        await interactor.ExecuteAsync(id, slotId);

        return NoContent();
    }

    [HttpPost("{id:guid}/slots/{slotId:guid}/mark-available")]
    [EndpointSummary("Marks an unavailable slot as available")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkSlotAvailable(
        [FromServices] MarkSlotAvailableInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid slotId)
    {
        await interactor.ExecuteAsync(id, slotId);

        return NoContent();
    }
}
