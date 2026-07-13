using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class CarAlreadyDeletedException : DomainException
{
    public CarAlreadyDeletedException(CarId id)
        : base($"Car {id.Value} is already deleted.")
    {
    }
}
