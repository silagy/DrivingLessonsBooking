using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.GetStudent;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Student;

[ApiController]
[Route("api/students")]
[Tags("Students")]
public class StudentQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a student")]
    [ProducesResponseType(typeof(GetStudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetStudentResponse> GetAsync(
        [FromServices] GetStudentInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

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
