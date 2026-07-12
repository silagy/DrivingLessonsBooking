using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherNameMustNotBeEmptyException : DomainException
{
    public TeacherNameMustNotBeEmptyException()
        : base("Teacher name must not be empty.")
    {
    }
}
