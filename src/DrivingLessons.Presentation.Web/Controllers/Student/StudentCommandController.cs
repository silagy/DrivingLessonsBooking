using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ChangeStudentCar;
using DrivingLessons.Application.Commands.ChangeStudentDetails;
using DrivingLessons.Application.Commands.ChangeStudentTeacher;
using DrivingLessons.Application.Commands.CreateStudent;
using DrivingLessons.Application.Commands.DeactivateStudent;
using DrivingLessons.Application.Commands.ReactivateStudent;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Student;

[ApiController]
[Route("api/students")]
[Tags("Students")]
public class StudentCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Add a student by hand, on one of their teacher's cars")]
    [ProducesResponseType(typeof(CreateStudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateStudentResponse>> CreateAsync(
        [FromServices] CreateStudentInteractor interactor,
        [FromBody] [Required] CreateStudentRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the student's national ID, name, phone, address, start date and license type")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeStudentDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeStudentDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [EndpointSummary("Deactivate the student so they can no longer submit on the student form")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(
        [FromServices] DeactivateStudentInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [EndpointSummary("Reactivate an inactive student so they can submit again")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateAsync(
        [FromServices] ReactivateStudentInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/change-teacher")]
    [EndpointSummary("Change the student's teacher, together with one of the new teacher's cars")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeTeacherAsync(
        [FromServices] ChangeStudentTeacherInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeStudentTeacherRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPost("{id:guid}/change-car")]
    [EndpointSummary("Change the student's car to another of their teacher's cars")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeCarAsync(
        [FromServices] ChangeStudentCarInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeStudentCarRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }
}
