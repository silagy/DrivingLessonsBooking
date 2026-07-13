using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.AssignCarToTeacher;
using DrivingLessons.Application.Commands.ChangeCarDetails;
using DrivingLessons.Application.Commands.CreateCar;
using DrivingLessons.Application.Commands.DeleteCar;
using DrivingLessons.Application.Commands.UnassignCarFromTeacher;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Car;

[ApiController]
[Route("api/cars")]
[Tags("Cars")]
public class CarCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a car")]
    [ProducesResponseType(typeof(CreateCarResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateCarResponse>> CreateAsync(
        [FromServices] CreateCarInteractor interactor,
        [FromBody] [Required] CreateCarRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the car's name, type, and transmission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeCarDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeCarDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete the car")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        [FromServices] DeleteCarInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/teachers/{teacherId:guid}")]
    [EndpointSummary("Assign the car to a teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTeacherAsync(
        [FromServices] AssignCarToTeacherInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid teacherId)
    {
        await interactor.ExecuteAsync(id, teacherId);

        return NoContent();
    }

    [HttpDelete("{id:guid}/teachers/{teacherId:guid}")]
    [EndpointSummary("Unassign the car from a teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnassignTeacherAsync(
        [FromServices] UnassignCarFromTeacherInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid teacherId)
    {
        await interactor.ExecuteAsync(id, teacherId);

        return NoContent();
    }
}
