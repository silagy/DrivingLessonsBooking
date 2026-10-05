using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationByLink;
using DrivingLessons.Application.Queries.GetPublicationDashboard;

namespace DrivingLessons.Application.Queries;

public interface IPublicationQueries
{
    Task<GetPublicationResponse?> GetByWeekAsync(DateOnly weekStart);

    Task<GetPublicationByLinkResponse?> GetByLinkTokenExcludingDraftsAsync(string linkToken);

    Task<GetPublicationDashboardResponse?> GetDashboardAsync(Guid publicationId, Guid teacherId);

    Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync(Guid? teacherId);

    Task<IReadOnlyList<Guid>> GetTeacherIdsWithScheduleForWeekAsync(DateOnly weekStart);
}
