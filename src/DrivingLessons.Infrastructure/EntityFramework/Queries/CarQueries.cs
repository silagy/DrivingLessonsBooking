using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindCars;
using DrivingLessons.Application.Queries.GetCar;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class CarQueries : ICarQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public CarQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetCarResponse?> GetAsync(Guid id)
    {
        var carId = CarId.Of(id);
        var selector = GetCarResponse.Selector(dbContext.Teachers);

        return await dbContext
                         .Cars
                         .Where(x => x.Id == carId)
                         .Select(selector)
                         .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyCollection<ItemForFindCarsResponse>> FindAsync()
    {
        var selector = ItemForFindCarsResponse.Selector(dbContext.Teachers);

        return await dbContext
                         .Cars
                         .Select(selector)
                         .ToListAsync();
    }
}
