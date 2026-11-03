using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindStudentsWithoutSubmission;

public class FindStudentsWithoutSubmissionInteractor
{
    private readonly IStudentQueries studentQueries;
    private readonly ICurrentUser currentUser;

    public FindStudentsWithoutSubmissionInteractor(IStudentQueries studentQueries, ICurrentUser currentUser)
    {
        this.studentQueries = studentQueries;
        this.currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>> ExecuteAsync(
        Guid publicationId,
        Guid teacherId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            throw new PublicationNotFoundException(resolvedPublicationId);
        }

        var students = await studentQueries.FindWithoutSubmissionAsync(publicationId, teacherId)
                       ?? throw new PublicationNotFoundException(resolvedPublicationId);

        return students;
    }
}
