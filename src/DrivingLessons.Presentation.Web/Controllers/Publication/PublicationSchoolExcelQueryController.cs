using DrivingLessons.Application.Queries.DownloadPublicationSchoolExcel;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Publication;

[ApiController]
[Route("api/publications")]
[Tags("Publications")]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
public class PublicationSchoolExcelQueryController : ControllerBase
{
    [HttpGet("{id:guid}/school-excel")]
    [EndpointSummary("Downloads the Excel workbook of every teacher's requests for a publication")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadSchoolExcel(
        [FromServices] DownloadPublicationSchoolExcelInteractor interactor,
        [FromRoute] Guid id)
    {
        var excel = await interactor.ExecuteAsync(id);

        return File(excel.Content, excel.ContentType, excel.FileName);
    }
}
