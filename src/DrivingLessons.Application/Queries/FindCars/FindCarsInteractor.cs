namespace DrivingLessons.Application.Queries.FindCars;

public class FindCarsInteractor
{
    private readonly ICarQueries queries;

    public FindCarsInteractor(ICarQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindCarsResponse>> ExecuteAsync()
    {
        return await queries.FindAsync();
    }
}
