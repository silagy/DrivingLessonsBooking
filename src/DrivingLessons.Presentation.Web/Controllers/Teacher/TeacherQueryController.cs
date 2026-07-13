using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Teacher;

[ApiController]
[Route("api/teachers")]
[Tags("Teachers")]
public class TeacherQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a teacher")]
    [ProducesResponseType(typeof(GetTeacherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetTeacherResponse> GetAsync(
        [FromServices] GetTeacherInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [EndpointSummary("Find all teachers")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindTeachersResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ItemForFindTeachersResponse>> FindAsync(
        [FromServices] FindTeachersInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
