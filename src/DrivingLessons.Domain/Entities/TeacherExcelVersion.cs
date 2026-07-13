using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class TeacherExcelVersion : Entity<TeacherExcelVersionId>
{
    public TeacherId TeacherId { get; private set; }
    public int Version { get; private set; }

    private TeacherExcelVersion()
    {
    }

    private TeacherExcelVersion(TeacherExcelVersionId id, TeacherId teacherId, int version)
        : base(id)
    {
        TeacherId = teacherId;
        Version = version;
    }

    internal static TeacherExcelVersion Create(TeacherId teacherId)
    {
        const int initialVersion = 1;
        var id = TeacherExcelVersionId.New();

        return new TeacherExcelVersion(id, teacherId, initialVersion);
    }

    internal void Increment()
    {
        Version += 1;
    }
}
