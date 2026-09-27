using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionMustBeForPublicationAndStudentException : DomainException
{
    public SubmissionMustBeForPublicationAndStudentException()
        : base("A submission can only be revised for its own publication and student.")
    {
    }
}
