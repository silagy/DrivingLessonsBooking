namespace DrivingLessons.Application.Queries.FindSlotRequests;

public class ItemForFindSlotRequestsResponse
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public int Rank { get; init; }
    public int PickCount { get; init; }
}
