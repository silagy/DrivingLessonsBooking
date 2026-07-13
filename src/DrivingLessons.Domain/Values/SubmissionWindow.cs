using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SubmissionWindow
{
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtc { get; }

    private SubmissionWindow(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public static SubmissionWindow Of(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new SubmissionWindowEndMustBeAfterStartException(startUtc, endUtc);
        }

        return new SubmissionWindow(startUtc, endUtc);
    }
}
