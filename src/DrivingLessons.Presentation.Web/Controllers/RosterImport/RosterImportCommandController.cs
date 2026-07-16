using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ImportRoster;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.RosterImport;

[ApiController]
[Route("api/roster-imports")]
[Tags("Roster Imports")]
public class RosterImportCommandController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1_048_576)]
    [EndpointSummary("Imports the Berosh roster CSV, upserting students and recording the import result")]
    [ProducesResponseType(typeof(ImportRosterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportRosterResponse>> ImportAsync(
        [FromServices] ImportRosterInteractor interactor,
        [Required] IFormFile file)
    {
        var content = file.OpenReadStream();
        var request = new ImportRosterRequest(file.FileName, content);

        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }
}
