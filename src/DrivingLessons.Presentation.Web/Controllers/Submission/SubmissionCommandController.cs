using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Queries.IdentifyStudent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Submission;

[ApiController]
[AllowAnonymous]
[Route("api/submissions")]
[Tags("Submissions")]
public class SubmissionCommandController : ControllerBase
{
    [HttpPost("by-link/{token}/identify")]
    [EndpointSummary("Identifies a roster student by national ID and returns their teacher, car and the teacher's week grid")]
    [ProducesResponseType(typeof(IdentifyStudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IdentifyStudentResponse> IdentifyAsync(
        [FromServices] IdentifyStudentInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] IdentifyStudentRequest request)
    {
        return await interactor.ExecuteAsync(token, request);
    }
}
