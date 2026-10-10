using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentTeacherMustNotBeDeletedException : DomainException
{
    public StudentTeacherMustNotBeDeletedException(StudentId studentId, TeacherId teacherId)
        : base($"Teacher {teacherId.Value} of student {studentId.Value} is deleted.")
    {
    }
}
