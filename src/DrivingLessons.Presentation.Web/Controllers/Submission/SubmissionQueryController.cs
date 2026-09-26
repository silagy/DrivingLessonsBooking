using DrivingLessons.Application.Queries.GetPublicationByLink;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Submission;

[ApiController]
[AllowAnonymous]
[Route("api/submissions")]
[Tags("Submissions")]
public class SubmissionQueryController : ControllerBase
{
    [HttpGet("by-link/{token}")]
    [EndpointSummary("Gets the week and submission window behind a student link; draft and unknown links are not found")]
    [ProducesResponseType(typeof(GetPublicationByLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetPublicationByLinkResponse> GetPublicationByLinkAsync(
        [FromServices] GetPublicationByLinkInteractor interactor,
        [FromRoute] string token)
    {
        return await interactor.ExecuteAsync(token);
    }
}
