using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.DownloadPublicationSchoolExcel;

public class DownloadPublicationSchoolExcelInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IExcelGenerator excelGenerator;

    public DownloadPublicationSchoolExcelInteractor(IPublicationRepository repository, IExcelGenerator excelGenerator)
    {
        this.repository = repository;
        this.excelGenerator = excelGenerator;
    }

    public async Task<ExcelFile> ExecuteAsync(Guid id)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        return await excelGenerator.GenerateForSchoolAsync(publication.Id);
    }
}
