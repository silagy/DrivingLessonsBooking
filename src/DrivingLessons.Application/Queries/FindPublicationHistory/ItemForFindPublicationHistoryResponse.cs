using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindPublicationHistory;

public class ItemForFindPublicationHistoryResponse
{
    public Guid PublicationId { get; init; }
    public DateOnly WeekStart { get; init; }
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public PublicationState State { get; init; }
    public DateTimeOffset? WindowStartUtc { get; init; }
    public DateTimeOffset? WindowEndUtc { get; init; }
    public int? LatestExcelVersion { get; init; }
}
