using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Abstractions;

public interface IExcelGenerator
{
    Task<ExcelFile> GenerateAsync(PublicationId publicationId, TeacherId teacherId);
}

public record ExcelFile(string FileName, byte[] Content, string ContentType);
