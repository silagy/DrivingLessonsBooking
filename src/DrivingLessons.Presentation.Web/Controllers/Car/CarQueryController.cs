using DrivingLessons.Application.Queries.FindCars;
using DrivingLessons.Application.Queries.GetCar;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Car;

[ApiController]
[Route("api/cars")]
[Tags("Cars")]
public class CarQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a car")]
    [ProducesResponseType(typeof(GetCarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetCarResponse> GetAsync(
        [FromServices] GetCarInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [EndpointSummary("Find all cars")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindCarsResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ItemForFindCarsResponse>> FindAsync(
        [FromServices] FindCarsInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
