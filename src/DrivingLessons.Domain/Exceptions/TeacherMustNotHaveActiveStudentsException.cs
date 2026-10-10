using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherMustNotHaveActiveStudentsException : DomainException
{
    public IReadOnlyList<StudentName> ActiveStudentNames { get; }

    public TeacherMustNotHaveActiveStudentsException(TeacherId id, IReadOnlyList<StudentName> activeStudentNames)
        : base($"Teacher {id.Value} still has {activeStudentNames.Count} active Students.")
    {
        ActiveStudentNames = activeStudentNames;
    }
}
