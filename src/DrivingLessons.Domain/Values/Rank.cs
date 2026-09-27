using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Rank
{
    private const int First = 1;

    public int Value { get; }

    private Rank(int value)
    {
        Value = value;
    }

    public static Rank Of(int value)
    {
        if (value < First)
        {
            throw new RankMustBePositiveException(value);
        }

        return new Rank(value);
    }
}
