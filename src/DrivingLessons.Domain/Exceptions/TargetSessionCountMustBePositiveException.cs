using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class TargetSessionCountMustBePositiveException : DomainException
{
    public TargetSessionCountMustBePositiveException(int value)
        : base($"Target session count must be at least 1 (was {value}).")
    {
    }
}
