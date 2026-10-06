using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateStudent;
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
}
