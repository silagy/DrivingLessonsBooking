using DrivingLessons.Application.Queries.DownloadPublicationExcel;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Publication;

[ApiController]
[Route("api/publications")]
[Tags("Publications")]
public class PublicationQueryController : ControllerBase
{
    [HttpGet("by-week")]
    [EndpointSummary("Gets the publication for a calendar week")]
    [ProducesResponseType(typeof(GetPublicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<GetPublicationResponse> GetByWeek(
        [FromServices] GetPublicationInteractor interactor,
        [FromQuery] DateOnly week)
    {
        return await interactor.ExecuteAsync(week);
    }

    [HttpGet("{id:guid}/dashboard")]
    [EndpointSummary("Gets the per-teacher dashboard of slot request counts for a publication")]
    [ProducesResponseType(typeof(GetPublicationDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<GetPublicationDashboardResponse> GetDashboard(
        [FromServices] GetPublicationDashboardInteractor interactor,
        [FromRoute] Guid id,
        [FromQuery] Guid teacherId)
    {
        return await interactor.ExecuteAsync(id, teacherId);
    }

    [HttpGet("history")]
    [EndpointSummary("Gets the history of past publications per teacher with state and latest Excel version")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemForFindPublicationHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistory(
        [FromServices] FindPublicationHistoryInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }

    [HttpGet("{id:guid}/excel")]
    [EndpointSummary("Downloads the teacher's Excel workbook for a publication")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadExcel(
        [FromServices] DownloadPublicationExcelInteractor interactor,
        [FromRoute] Guid id,
        [FromQuery] Guid teacherId)
    {
        var excel = await interactor.ExecuteAsync(id, teacherId);

        return File(excel.Content, excel.ContentType, excel.FileName);
    }
}
