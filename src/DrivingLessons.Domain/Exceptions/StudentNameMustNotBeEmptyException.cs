using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class StudentNameMustNotBeEmptyException : DomainException
{
    public StudentNameMustNotBeEmptyException()
        : base("Student name must not be empty.")
    {
    }
}
