using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentAlreadyWithTeacherException : DomainException
{
    public StudentAlreadyWithTeacherException(StudentId id, TeacherId teacherId)
        : base($"Student {id.Value} is already with teacher {teacherId.Value}.")
    {
    }
}
