namespace DrivingLessons.Infrastructure.Email;

public class SentTeacherExcelEmail
{
    public Guid PublicationId { get; private set; }

    public Guid TeacherId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset SentAtUtc { get; private set; }

    private SentTeacherExcelEmail()
    {
    }

    private SentTeacherExcelEmail(Guid publicationId, Guid teacherId, int version, DateTimeOffset sentAtUtc)
    {
        PublicationId = publicationId;
        TeacherId = teacherId;
        Version = version;
        SentAtUtc = sentAtUtc;
    }

    public static SentTeacherExcelEmail Create(Guid publicationId, Guid teacherId, int version, DateTimeOffset sentAtUtc)
    {
        return new SentTeacherExcelEmail(publicationId, teacherId, version, sentAtUtc);
    }
}
