using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record TeacherId : EntityId
{
    private TeacherId(Guid value)
        : base(value)
    {
    }

    public static TeacherId New()
    {
        return new TeacherId(Guid.NewGuid());
    }

    public static TeacherId Of(Guid value)
    {
        return new TeacherId(value);
    }
}
