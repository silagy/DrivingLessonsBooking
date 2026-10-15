using DrivingLessons.Application.Auth;
using DrivingLessons.Presentation.Web.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DrivingLessons.Presentation.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(LoginInteractor login) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AdminSignIn)]
    public Task<LoginResult> Login([FromBody] LoginCommand command) =>
        login.ExecuteAsync(command);
}
