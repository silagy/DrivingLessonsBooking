using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionAlreadyExistsException : DomainException
{
    public SubmissionAlreadyExistsException()
        : base("A submission already exists for this student and week.")
    {
    }
}
