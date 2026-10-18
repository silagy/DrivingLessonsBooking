using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindSlotRequests;

public class FindSlotRequestsInteractor
{
    private readonly ISubmissionQueries submissionQueries;
    private readonly ICurrentUser currentUser;

    public FindSlotRequestsInteractor(ISubmissionQueries submissionQueries, ICurrentUser currentUser)
    {
        this.submissionQueries = submissionQueries;
        this.currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ItemForFindSlotRequestsResponse>> ExecuteAsync(
        Guid publicationId,
        Guid teacherId,
        Guid slotId)
    {
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            var resolvedPublicationId = PublicationId.Of(publicationId);

            throw new PublicationNotFoundException(resolvedPublicationId);
        }

        return await submissionQueries.FindSlotRequestsAsync(publicationId, teacherId, slotId);
    }
}
