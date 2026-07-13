using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record TeacherExcelVersionId : EntityId
{
    private TeacherExcelVersionId(Guid value)
        : base(value)
    {
    }

    public static TeacherExcelVersionId New()
    {
        return new TeacherExcelVersionId(Guid.NewGuid());
    }

    public static TeacherExcelVersionId Of(Guid value)
    {
        return new TeacherExcelVersionId(value);
    }
}
