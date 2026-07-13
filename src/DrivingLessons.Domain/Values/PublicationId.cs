using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record PublicationId : EntityId
{
    private PublicationId(Guid value)
        : base(value)
    {
    }

    public static PublicationId New()
    {
        return new PublicationId(Guid.NewGuid());
    }

    public static PublicationId Of(Guid value)
    {
        return new PublicationId(value);
    }
}
