using DrivingLessons.Application.Queries.GetLatestRosterImport;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.RosterImport;

[ApiController]
[Route("api/roster-imports")]
[Tags("Roster Imports")]
public class RosterImportQueryController : ControllerBase
{
    [HttpGet("latest")]
    [EndpointSummary("Gets the most recent roster import with its entries and failures")]
    [ProducesResponseType(typeof(GetLatestRosterImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetLatestRosterImportResponse> GetLatestAsync(
        [FromServices] GetLatestRosterImportInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
