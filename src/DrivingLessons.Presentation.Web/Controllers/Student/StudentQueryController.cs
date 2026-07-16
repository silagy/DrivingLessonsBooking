using DrivingLessons.Application.Queries.FindStudents;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Student;

[ApiController]
[Route("api/students")]
[Tags("Students")]
public class StudentQueryController : ControllerBase
{
    [HttpGet("find")]
    [EndpointSummary("Finds all students, optionally filtered by teacher")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindStudentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(
        [FromServices] FindStudentsInteractor interactor,
        [FromQuery] Guid? teacherId)
    {
        return await interactor.ExecuteAsync(teacherId);
    }
}
