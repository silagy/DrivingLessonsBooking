using DrivingLessons.Application.Queries;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class SubmissionQueries : ISubmissionQueries
{
    public Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId)
    {
        IReadOnlyDictionary<Guid, int> counts = new Dictionary<Guid, int>();

        return Task.FromResult(counts);
    }

    public Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId)
    {
        var stats = new SubmissionStats(0, 0, null);

        return Task.FromResult(stats);
    }
}
