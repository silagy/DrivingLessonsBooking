using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionWindowMustBeOpenException : DomainException
{
    public SubmissionWindowMustBeOpenException()
        : base("Submissions are accepted only while the week's submission window is open.")
    {
    }
}
