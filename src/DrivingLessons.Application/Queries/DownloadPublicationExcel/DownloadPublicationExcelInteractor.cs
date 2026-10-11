using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.DownloadPublicationExcel;

public class DownloadPublicationExcelInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IExcelGenerator excelGenerator;
    private readonly ICurrentUser currentUser;

    public DownloadPublicationExcelInteractor(
        IPublicationRepository repository,
        IExcelGenerator excelGenerator,
        ICurrentUser currentUser)
    {
        this.repository = repository;
        this.excelGenerator = excelGenerator;
        this.currentUser = currentUser;
    }

    public async Task<ExcelFile> ExecuteAsync(Guid id, Guid teacherId)
    {
        var publicationId = PublicationId.Of(id);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            throw new PublicationNotFoundException(publicationId);
        }

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        return await excelGenerator.GenerateAsync(publication.Id, resolvedTeacherId);
    }
}
