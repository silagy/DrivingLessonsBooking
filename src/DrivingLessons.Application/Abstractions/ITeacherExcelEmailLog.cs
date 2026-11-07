using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Abstractions;

public interface ITeacherExcelEmailLog
{
    Task<bool> WasSentAsync(PublicationId publicationId, TeacherId teacherId, int version);

    void RecordSent(PublicationId publicationId, TeacherId teacherId, int version, DateTimeOffset sentAtUtc);
}
