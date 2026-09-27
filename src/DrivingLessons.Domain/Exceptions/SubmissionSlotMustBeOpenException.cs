using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionSlotMustBeOpenException : DomainException
{
    public SubmissionSlotMustBeOpenException()
        : base("Unavailable slots cannot be requested.")
    {
    }
}
