using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record TeacherAssignmentId : EntityId
{
    private TeacherAssignmentId(Guid value)
        : base(value)
    {
    }

    public static TeacherAssignmentId New()
    {
        return new TeacherAssignmentId(Guid.NewGuid());
    }

    public static TeacherAssignmentId Of(Guid value)
    {
        return new TeacherAssignmentId(value);
    }
}
