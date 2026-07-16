using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface ICarRepository
{
    Task<Car?> GetAsync(CarId id);

    Task<IReadOnlyCollection<Car>> FindActiveAsync();

    void Add(Car car);
}
