using DrivingLessons.Application.Queries;

namespace DrivingLessons.Application.Queries.FindPublicationHistory;

public class FindPublicationHistoryInteractor
{
    private readonly IPublicationQueries queries;

    public FindPublicationHistoryInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> ExecuteAsync()
    {
        return await queries.FindHistoryAsync();
    }
}
