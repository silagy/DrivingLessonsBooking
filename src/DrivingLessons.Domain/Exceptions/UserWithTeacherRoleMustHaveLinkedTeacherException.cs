using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserWithTeacherRoleMustHaveLinkedTeacherException : DomainException
{
    public UserWithTeacherRoleMustHaveLinkedTeacherException()
        : base("A User with the Teacher Role must be linked to a Teacher.")
    {
    }
}
