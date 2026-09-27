using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record TargetSessionCount
{
    private const int Minimum = 1;

    public int Value { get; }

    private TargetSessionCount(int value)
    {
        Value = value;
    }

    public static TargetSessionCount Of(int value)
    {
        if (value < Minimum)
        {
            throw new TargetSessionCountMustBePositiveException(value);
        }

        return new TargetSessionCount(value);
    }

    public bool IsCoveredBy(int pickCount)
    {
        return pickCount >= Value;
    }
}
