using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Abstractions;

public interface IExcelGenerator
{
    Task<ExcelFile> GenerateAsync(PublicationId publicationId, TeacherId teacherId);

    Task<ExcelFile> GenerateForSchoolAsync(PublicationId publicationId);
}

public record ExcelFile(string FileName, byte[] Content, string ContentType);
