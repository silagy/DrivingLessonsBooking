using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyCollection<Car>> FindActiveAsync()
    {
        return await dbContext.Cars.ToListAsync();
    }

    public void Add(Car car)
    {
        dbContext.Cars.Add(car);
    }
}
