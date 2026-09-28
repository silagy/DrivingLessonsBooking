using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionSlotMustBeRequestedOnceException : DomainException
{
    public SubmissionSlotMustBeRequestedOnceException()
        : base("A slot can be requested at most once per submission.")
    {
    }
}
