using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherAssignmentMustNotHaveActiveStudentsException : DomainException
{
    public TeacherName TeacherName { get; }
    public IReadOnlyList<StudentName> ActiveStudentNames { get; }

    public TeacherAssignmentMustNotHaveActiveStudentsException(
        CarId carId,
        TeacherId teacherId,
        TeacherName teacherName,
        IReadOnlyList<StudentName> activeStudentNames)
        : base($"Teacher {teacherId.Value} still has {activeStudentNames.Count} active Students on car {carId.Value}.")
    {
        TeacherName = teacherName;
        ActiveStudentNames = activeStudentNames;
    }
}
