using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Car : Entity<CarId>
{
    public CarName Name { get; private set; }
    public CarType Type { get; private set; }
    public Transmission Transmission { get; private set; }

    private Car()
    {
    }

    private Car(CarId id, CarName name, CarType type, Transmission transmission)
        : base(id)
    {
        Name = name;
        Type = type;
        Transmission = transmission;
    }

    internal static Car Create(CarName name, CarType type, Transmission transmission)
    {
        var id = CarId.New();
        return new Car(id, name, type, transmission);
    }

    internal void ChangeDetails(CarName name, CarType type, Transmission transmission)
    {
        Name = name;
        Type = type;
        Transmission = transmission;
    }
}
