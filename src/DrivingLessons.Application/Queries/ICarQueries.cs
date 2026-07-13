using DrivingLessons.Application.Queries.FindCars;
using DrivingLessons.Application.Queries.GetCar;

namespace DrivingLessons.Application.Queries;

public interface ICarQueries
{
    Task<GetCarResponse?> GetAsync(Guid id);

    Task<IReadOnlyCollection<ItemForFindCarsResponse>> FindAsync();
}
