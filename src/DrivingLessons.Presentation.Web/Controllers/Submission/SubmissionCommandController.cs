using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateSubmission;
using DrivingLessons.Application.Commands.ReviseSubmission;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Presentation.Web.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DrivingLessons.Presentation.Web.Controllers.Submission;

[ApiController]
[AllowAnonymous]
[Route("api/submissions")]
[Tags("Submissions")]
public class SubmissionCommandController : ControllerBase
{
    [HttpPost("by-link/{token}/identify")]
    [EnableRateLimiting(RateLimitPolicies.StudentIdentify)]
    [EndpointSummary("Identifies a roster student by national ID and returns their teacher, car and the teacher's week grid")]
    [ProducesResponseType(typeof(IdentifyStudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IdentifyStudentResponse> IdentifyAsync(
        [FromServices] IdentifyStudentInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] IdentifyStudentRequest request)
    {
        return await interactor.ExecuteAsync(token, request);
    }

    [HttpPost("by-link/{token}")]
    [EnableRateLimiting(RateLimitPolicies.StudentIdentify)]
    [EndpointSummary("Creates the student's submission for the week; fails if one already exists")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateAsync(
        [FromServices] CreateSubmissionInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] CreateSubmissionRequest request)
    {
        await interactor.ExecuteAsync(token, request);

        return NoContent();
    }

    [HttpPut("by-link/{token}")]
    [EnableRateLimiting(RateLimitPolicies.StudentIdentify)]
    [EndpointSummary("Replaces the student's submission for the week; fails if there is none yet")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReviseAsync(
        [FromServices] ReviseSubmissionInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] ReviseSubmissionRequest request)
    {
        await interactor.ExecuteAsync(token, request);

        return NoContent();
    }
}
