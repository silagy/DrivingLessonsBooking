using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class CarRepository : ICarRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public CarRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Car?> GetAsync(CarId id)
    {
        return await dbContext.Cars.FindAsync(id);
    }

    public void Add(Car car)
    {
        dbContext.Cars.Add(car);
    }
}
