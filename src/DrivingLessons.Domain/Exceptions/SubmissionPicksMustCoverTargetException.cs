using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionPicksMustCoverTargetException : DomainException
{
    public SubmissionPicksMustCoverTargetException(TargetSessionCount targetCount, int pickCount)
        : base($"A submission needs at least {targetCount.Value} slot requests to cover its target; "
               + $"it has {pickCount}.")
    {
    }
}
