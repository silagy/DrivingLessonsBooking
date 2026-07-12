using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.AddCar;
using DrivingLessons.Application.Commands.ChangeCarDetails;
using DrivingLessons.Application.Commands.ChangeTeacherDetails;
using DrivingLessons.Application.Commands.CreateTeacher;
using DrivingLessons.Application.Commands.DeleteTeacher;
using DrivingLessons.Application.Commands.RemoveCar;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Teacher;

[ApiController]
[Route("api/teachers")]
[Tags("Teachers")]
public class TeacherCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a teacher")]
    [ProducesResponseType(typeof(CreateTeacherResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTeacherResponse>> CreateAsync(
        [FromServices] CreateTeacherInteractor interactor,
        [FromBody] [Required] CreateTeacherRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPost("{id:guid}/cars")]
    [EndpointSummary("Add a car to the teacher")]
    [ProducesResponseType(typeof(AddCarResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddCarResponse>> AddCarAsync(
        [FromServices] AddCarInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] AddCarRequest request)
    {
        var result = await interactor.ExecuteAsync(id, request);

        return CreatedAtAction(null, result);
    }

    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the teacher's name and contact email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeTeacherDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeTeacherDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPut("{id:guid}/cars/{carId:guid}")]
    [EndpointSummary("Change a car's name, type, and transmission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeCarDetailsAsync(
        [FromServices] ChangeCarDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid carId,
        [FromBody] [Required] ChangeCarDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, carId, request);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete the teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        [FromServices] DeleteTeacherInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpDelete("{id:guid}/cars/{carId:guid}")]
    [EndpointSummary("Remove a car from the teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveCarAsync(
        [FromServices] RemoveCarInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid carId)
    {
        await interactor.ExecuteAsync(id, carId);

        return NoContent();
    }
}
