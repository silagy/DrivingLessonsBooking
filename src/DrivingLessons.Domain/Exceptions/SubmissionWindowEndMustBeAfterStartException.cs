using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionWindowEndMustBeAfterStartException : DomainException
{
    public SubmissionWindowEndMustBeAfterStartException(DateTimeOffset startUtc, DateTimeOffset endUtc)
        : base($"Submission window end {endUtc:O} must be after start {startUtc:O}.")
    {
    }
}
