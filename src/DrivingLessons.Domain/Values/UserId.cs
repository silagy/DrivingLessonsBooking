using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record UserId : EntityId
{
    private UserId(Guid value)
        : base(value)
    {
    }

    public static UserId New()
    {
        return new UserId(Guid.NewGuid());
    }

    public static UserId Of(Guid value)
    {
        return new UserId(value);
    }
}
