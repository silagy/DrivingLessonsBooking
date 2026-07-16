using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record RosterImportId : EntityId
{
    private RosterImportId(Guid value)
        : base(value)
    {
    }

    public static RosterImportId New()
    {
        return new RosterImportId(Guid.NewGuid());
    }

    public static RosterImportId Of(Guid value)
    {
        return new RosterImportId(value);
    }
}
