namespace DrivingLessons.Application.Queries.FindStudentsWithoutSubmission;

public class ItemForFindStudentsWithoutSubmissionResponse
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? WhatsAppNumber { get; init; }
}
