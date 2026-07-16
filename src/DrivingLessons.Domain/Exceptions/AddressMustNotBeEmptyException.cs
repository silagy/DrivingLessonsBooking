using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class AddressMustNotBeEmptyException : DomainException
{
    public AddressMustNotBeEmptyException()
        : base("Address must not be empty.")
    {
    }
}
