using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindStudentsWithoutSubmission;

public class FindSchoolStudentsWithoutSubmissionInteractor
{
    private readonly IStudentQueries studentQueries;

    public FindSchoolStudentsWithoutSubmissionInteractor(IStudentQueries studentQueries)
    {
        this.studentQueries = studentQueries;
    }

    public async Task<IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>> ExecuteAsync(Guid publicationId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);

        var students = await studentQueries.FindWithoutSubmissionAsync(publicationId, null)
                       ?? throw new PublicationNotFoundException(resolvedPublicationId);

        return students;
    }
}
