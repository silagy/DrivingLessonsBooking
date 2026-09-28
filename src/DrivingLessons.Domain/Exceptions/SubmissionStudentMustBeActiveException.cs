using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionStudentMustBeActiveException : DomainException
{
    public SubmissionStudentMustBeActiveException()
        : base("Submissions are accepted only from students on the current roster.")
    {
    }
}
