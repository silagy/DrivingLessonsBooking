using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationDashboard;

public class GetPublicationDashboardInteractor
{
    private readonly IPublicationQueries publicationQueries;
    private readonly ISubmissionQueries submissionQueries;

    public GetPublicationDashboardInteractor(
        IPublicationQueries publicationQueries,
        ISubmissionQueries submissionQueries)
    {
        this.publicationQueries = publicationQueries;
        this.submissionQueries = submissionQueries;
    }

    public async Task<GetPublicationDashboardResponse> ExecuteAsync(Guid publicationId, Guid teacherId)
    {
        var dashboard = await publicationQueries.GetDashboardAsync(publicationId, teacherId);

        if (dashboard is null)
        {
            throw new PublicationNotFoundException(PublicationId.Of(publicationId));
        }

        var counts = await submissionQueries.GetSlotRequestCountsAsync(publicationId, teacherId);
        var stats = await submissionQueries.GetStatsAsync(publicationId, teacherId);

        var slotCounts = dashboard.SlotCounts
                                  .Select(slot => new SlotCountForGetPublicationDashboardResponse
                                  {
                                      SlotId = slot.SlotId,
                                      Day = slot.Day,
                                      Window = slot.Window,
                                      State = slot.State,
                                      RequestCount = counts.TryGetValue(slot.SlotId, out var count)
                                          ? count
                                          : 0
                                  })
                                  .ToList();

        return new GetPublicationDashboardResponse
        {
            State = dashboard.State,
            WindowStartUtc = dashboard.WindowStartUtc,
            WindowEndUtc = dashboard.WindowEndUtc,
            LinkToken = dashboard.LinkToken,
            StudentsSubmitted = stats.StudentsSubmitted,
            TotalPicks = stats.TotalPicks,
            LastSubmissionAtUtc = stats.LastSubmissionAtUtc,
            LatestExcelVersion = dashboard.LatestExcelVersion,
            SlotCounts = slotCounts
        };
    }
}
