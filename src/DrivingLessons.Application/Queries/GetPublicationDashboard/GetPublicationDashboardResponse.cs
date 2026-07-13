using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationDashboard;

public class GetPublicationDashboardResponse
{
    public PublicationState State { get; init; }
    public DateTimeOffset? WindowStartUtc { get; init; }
    public DateTimeOffset? WindowEndUtc { get; init; }
    public string LinkToken { get; init; } = string.Empty;
    public int StudentsSubmitted { get; init; }
    public int TotalPicks { get; init; }
    public DateTimeOffset? LastSubmissionAtUtc { get; init; }
    public int? LatestExcelVersion { get; init; }
    public IReadOnlyList<SlotCountForGetPublicationDashboardResponse> SlotCounts { get; init; } = [];
}

public class SlotCountForGetPublicationDashboardResponse
{
    public Guid SlotId { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public int RequestCount { get; init; }
}
