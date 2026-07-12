using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record CarId : EntityId
{
    private CarId(Guid value)
        : base(value)
    {
    }

    public static CarId New()
    {
        return new CarId(Guid.NewGuid());
    }

    public static CarId Of(Guid value)
    {
        return new CarId(value);
    }
}
