using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class TeacherAssignment : Entity<TeacherAssignmentId>
{
    public TeacherId TeacherId { get; private set; }

    private TeacherAssignment()
    {
    }

    private TeacherAssignment(TeacherAssignmentId id, TeacherId teacherId)
        : base(id)
    {
        TeacherId = teacherId;
    }

    internal static TeacherAssignment Create(TeacherId teacherId)
    {
        var id = TeacherAssignmentId.New();

        return new TeacherAssignment(id, teacherId);
    }
}
