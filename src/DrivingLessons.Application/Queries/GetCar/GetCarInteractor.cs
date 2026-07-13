using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetCar;

public class GetCarInteractor
{
    private readonly ICarQueries queries;

    public GetCarInteractor(ICarQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetCarResponse> ExecuteAsync(Guid id)
    {
        var car = await queries.GetAsync(id);

        if (car is null)
        {
            var carId = CarId.Of(id);
            throw new CarNotFoundException(carId);
        }

        return car;
    }
}
