using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ChangeMyPassword;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Me;

[ApiController]
[Route("api/me")]
[Tags("Me")]
[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]
public class MeCommandController : ControllerBase
{
    [HttpPut("password")]
    [EndpointSummary("Change the signed-in user's password; returns a fresh token so they stay signed in")]
    [ProducesResponseType(typeof(ChangeMyPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChangeMyPasswordResponse>> ChangePasswordAsync(
        [FromServices] ChangeMyPasswordInteractor interactor,
        [FromBody] [Required] ChangeMyPasswordRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return Ok(result);
    }
}
