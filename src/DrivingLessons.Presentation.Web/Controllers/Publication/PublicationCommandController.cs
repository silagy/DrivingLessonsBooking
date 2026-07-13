using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ExtendPublicationWindow;
using DrivingLessons.Application.Commands.PublishPublication;
using DrivingLessons.Application.Commands.ReopenPublication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Publication;

[ApiController]
[Route("api/publications")]
[Tags("Publications")]
public class PublicationCommandController : ControllerBase
{
    [HttpPost("{id:guid}/publish")]
    [EndpointSummary("Publishes a draft publication with a submission window and schedules its open/close jobs")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(
        [FromServices] PublishPublicationInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] PublishPublicationRequest request)
    {
        await interactor.ExecuteAsync(id, request.StartUtc, request.EndUtc);

        return NoContent();
    }

    [HttpPost("{id:guid}/extend-window")]
    [EndpointSummary("Extends the submission window end of an open publication and reschedules its close job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExtendWindow(
        [FromServices] ExtendPublicationWindowInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ExtendPublicationWindowRequest request)
    {
        await interactor.ExecuteAsync(id, request.NewEndUtc);

        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    [EndpointSummary("Reopens a closed publication with a new window end and reschedules its close job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reopen(
        [FromServices] ReopenPublicationInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ReopenPublicationRequest request)
    {
        await interactor.ExecuteAsync(id, request.NewEndUtc);

        return NoContent();
    }
}
