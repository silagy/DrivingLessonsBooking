using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class CarTypeMustNotBeEmptyException : DomainException
{
    public CarTypeMustNotBeEmptyException()
        : base("Car type must not be empty.")
    {
    }
}
