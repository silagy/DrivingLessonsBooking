using DrivingLessons.Application.Queries.GetMe;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Me;

[ApiController]
[Route("api/me")]
[Tags("Me")]
[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]
public class MeQueryController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get the signed-in user, with the linked Teacher's name")]
    [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetUserResponse> GetAsync([FromServices] GetMeInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
