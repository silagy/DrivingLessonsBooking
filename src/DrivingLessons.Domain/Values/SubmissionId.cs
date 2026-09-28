using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record SubmissionId : EntityId
{
    private SubmissionId(Guid value)
        : base(value)
    {
    }

    public static SubmissionId New()
    {
        return new SubmissionId(Guid.NewGuid());
    }

    public static SubmissionId Of(Guid value)
    {
        return new SubmissionId(value);
    }
}
