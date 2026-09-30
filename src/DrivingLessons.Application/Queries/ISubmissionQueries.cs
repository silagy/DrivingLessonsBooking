using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface ISubmissionQueries
{
    Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId);

    Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId);
}

public record SubmissionStats(int StudentsSubmitted, int TotalPicks, DateTimeOffset? LastSubmissionAtUtc);

public record SlotRequestDetail(
    DayOfWeek Day,
    SlotWindowType Window,
    string StudentName,
    string NationalId,
    string Phone,
    Transmission Transmission,
    SessionType SessionType,
    int Rank,
    int TargetCount,
    string? Constraint);
