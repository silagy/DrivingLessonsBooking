using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RankMustBePositiveException : DomainException
{
    public RankMustBePositiveException(int value)
        : base($"Rank must be at least 1 (was {value}).")
    {
    }
}
