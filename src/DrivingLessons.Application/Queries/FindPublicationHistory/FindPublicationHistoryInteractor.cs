using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindPublicationHistory;

public class FindPublicationHistoryInteractor
{
    private readonly IPublicationQueries queries;
    private readonly ICurrentUser currentUser;

    public FindPublicationHistoryInteractor(IPublicationQueries queries, ICurrentUser currentUser)
    {
        this.queries = queries;
        this.currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> ExecuteAsync()
    {
        if (currentUser.Role is Role.Administrator)
        {
            return await queries.FindHistoryAsync(null);
        }

        var linkedTeacherId = currentUser.TeacherId;

        if (linkedTeacherId is null)
        {
            return [];
        }

        return await queries.FindHistoryAsync(linkedTeacherId.Value);
    }
}
