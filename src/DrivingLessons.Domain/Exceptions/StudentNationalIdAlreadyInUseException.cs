using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentNationalIdAlreadyInUseException : DomainException
{
    public StudentName ExistingStudentName { get; }

    public StudentNationalIdAlreadyInUseException(StudentName existingStudentName)
        : base("Another Student already has this national ID.")
    {
        ExistingStudentName = existingStudentName;
    }
}
