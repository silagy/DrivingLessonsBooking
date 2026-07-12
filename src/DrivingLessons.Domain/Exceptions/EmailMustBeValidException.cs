using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class EmailMustBeValidException : DomainException
{
    public EmailMustBeValidException()
        : base("Email must be a valid email address.")
    {
    }
}
