using DrivingLessons.Application.Queries.FindUsers;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("Users")]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
public class UserQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a user")]
    [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetUserResponse> GetAsync(
        [FromServices] GetUserInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [EndpointSummary("Find all users, active users first")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindUsersResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ItemForFindUsersResponse>> FindAsync(
        [FromServices] FindUsersInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
