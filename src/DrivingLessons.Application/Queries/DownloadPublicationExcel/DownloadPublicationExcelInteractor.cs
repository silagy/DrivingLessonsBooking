using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.DownloadPublicationExcel;

public class DownloadPublicationExcelInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IExcelGenerator excelGenerator;

    public DownloadPublicationExcelInteractor(IPublicationRepository repository, IExcelGenerator excelGenerator)
    {
        this.repository = repository;
        this.excelGenerator = excelGenerator;
    }

    public async Task<ExcelFile> ExecuteAsync(Guid id, Guid teacherId)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var resolvedTeacherId = TeacherId.Of(teacherId);

        return await excelGenerator.GenerateAsync(publication.Id, resolvedTeacherId);
    }
}
