namespace DrivingLessons.Application.Queries.FindUsers;

public class FindUsersInteractor
{
    private readonly IUserQueries queries;

    public FindUsersInteractor(IUserQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindUsersResponse>> ExecuteAsync()
    {
        return await queries.FindAsync();
    }
}
