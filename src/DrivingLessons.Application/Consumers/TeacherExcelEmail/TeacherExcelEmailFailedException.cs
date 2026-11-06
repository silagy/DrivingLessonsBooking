using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Consumers.TeacherExcelEmail;

public class TeacherExcelEmailFailedException : Exception
{
    public PublicationId PublicationId { get; }

    public IReadOnlyList<TeacherId> TeacherIds { get; }

    public TeacherExcelEmailFailedException(PublicationId publicationId, IReadOnlyList<TeacherId> teacherIds)
        : base($"Excel email failed for [{teacherIds.Count}] teacher(s) of publication [{publicationId.Value}].")
    {
        PublicationId = publicationId;
        TeacherIds = teacherIds;
    }
}
