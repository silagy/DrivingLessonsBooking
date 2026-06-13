using DrivingLessons.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(LoginInteractor login) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken) =>
        login.Handle(command, cancellationToken);
}
