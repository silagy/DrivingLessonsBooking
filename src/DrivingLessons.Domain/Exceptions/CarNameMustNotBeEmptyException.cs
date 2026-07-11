using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class CarNameMustNotBeEmptyException : DomainException
{
    public CarNameMustNotBeEmptyException()
        : base("Car name must not be empty.")
    {
    }
}
