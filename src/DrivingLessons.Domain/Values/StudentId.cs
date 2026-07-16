using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record StudentId : EntityId
{
    private StudentId(Guid value)
        : base(value)
    {
    }

    public static StudentId New()
    {
        return new StudentId(Guid.NewGuid());
    }

    public static StudentId Of(Guid value)
    {
        return new StudentId(value);
    }
}
