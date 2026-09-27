using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.IdentifyStudent;

public class IdentifyStudentInteractor
{
    private readonly IPublicationQueries publicationQueries;
    private readonly IStudentQueries studentQueries;

    public IdentifyStudentInteractor(IPublicationQueries publicationQueries, IStudentQueries studentQueries)
    {
        this.publicationQueries = publicationQueries;
        this.studentQueries = studentQueries;
    }

    public async Task<IdentifyStudentResponse> ExecuteAsync(string linkToken, IdentifyStudentRequest request)
    {
        var publication = await publicationQueries.GetByLinkTokenExcludingDraftsAsync(linkToken)
                          ?? throw new PublicationLinkNotFoundException();

        var nationalId = NationalId.Of(request.NationalId);

        var student = await studentQueries.GetActiveByNationalIdAsync(nationalId, publication.WeekStart)
                      ?? throw new StudentNotFoundException();

        return student;
    }
}
