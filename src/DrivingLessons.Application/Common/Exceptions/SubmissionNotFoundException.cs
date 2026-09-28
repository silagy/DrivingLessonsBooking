namespace DrivingLessons.Application.Common.Exceptions;

public class SubmissionNotFoundException : NotFoundException
{
    public SubmissionNotFoundException()
        : base("No submission exists for this student and week yet.")
    {
    }
}
