using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateUser;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("Users")]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
public class UserCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a user who can sign in with a temporary password")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResponse>> CreateAsync(
        [FromServices] CreateUserInteractor interactor,
        [FromBody] [Required] CreateUserRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }
}
