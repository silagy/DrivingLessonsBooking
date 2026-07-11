using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Teacher : AggregateRoot<TeacherId>
{
    private readonly List<Car> cars = [];

    public TeacherName Name { get; private set; }
    public Email ContactEmail { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Car> Cars => cars.AsReadOnly();

    private Teacher()
    {
    }

    private Teacher(TeacherId id, TeacherName name, Email contactEmail, bool isDeleted)
        : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;
        IsDeleted = isDeleted;

        var createdEvent = new TeacherCreated(id, name, contactEmail);
        AddEvent(createdEvent);
    }

    public static Teacher Create(TeacherName name, Email contactEmail)
    {
        const bool isDeleted = false;
        var id = TeacherId.New();

        return new Teacher(id, name, contactEmail, isDeleted);
    }

    public Car AddCar(CarName name, CarType type, Transmission transmission)
    {
        var car = Car.Create(name, type, transmission);
        cars.Add(car);

        AddEvent(new CarAdded(Id, car.Id, name, type, transmission));

        return car;
    }

    public void ChangeDetails(TeacherName name, Email contactEmail)
    {
        Name = name;
        ContactEmail = contactEmail;

        AddEvent(new TeacherDetailsChanged(Id, name, contactEmail));
    }

    public void ChangeCarDetails(Car car, CarName name, CarType type, Transmission transmission)
    {
        MustOwnCar(car);

        car.ChangeDetails(name, type, transmission);

        AddEvent(new CarDetailsChanged(Id, car.Id, name, type, transmission));
    }

    public void Delete()
    {
        MustNotBeDeleted();

        IsDeleted = true;

        AddEvent(new TeacherDeleted(Id));
    }

    private void MustOwnCar(Car car)
    {
        if (!cars.Contains(car))
        {
            throw new CarNotInTeacherException(Id, car.Id);
        }
    }

    private void MustNotBeDeleted()
    {
        if (IsDeleted)
        {
            throw new TeacherAlreadyDeletedException(Id);
        }
    }
}
