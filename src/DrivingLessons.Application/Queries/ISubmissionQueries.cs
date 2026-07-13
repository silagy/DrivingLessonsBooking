namespace DrivingLessons.Application.Queries;

public interface ISubmissionQueries
{
    Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId);

    Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId);
}

public record SubmissionStats(int StudentsSubmitted, int TotalPicks, DateTimeOffset? LastSubmissionAtUtc);
