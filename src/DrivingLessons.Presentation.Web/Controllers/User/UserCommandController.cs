using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ChangeUserDetails;
using DrivingLessons.Application.Commands.ChangeUserRole;
using DrivingLessons.Application.Commands.CreateUser;
using DrivingLessons.Application.Commands.DeleteUser;
using DrivingLessons.Application.Commands.RestoreUser;
using DrivingLessons.Application.Commands.SetUserTemporaryPassword;
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

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete the user so they can no longer sign in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        [FromServices] DeleteUserInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [EndpointSummary("Restore a deleted user so they can sign in again")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreAsync(
        [FromServices] RestoreUserInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the user's name and sign-in email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeUserDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeUserDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    [EndpointSummary("Change the user's role; they are signed out on their next request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeRoleAsync(
        [FromServices] ChangeUserRoleInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeUserRoleRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPut("{id:guid}/temporary-password")]
    [EndpointSummary("Set a new temporary password; the user is signed out on their next request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetTemporaryPasswordAsync(
        [FromServices] SetUserTemporaryPasswordInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] SetUserTemporaryPasswordRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }
}
