using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class CarNotFoundException : NotFoundException
{
    public CarNotFoundException(CarId id)
        : base($"Car {id.Value} was not found.")
    {
    }
}
