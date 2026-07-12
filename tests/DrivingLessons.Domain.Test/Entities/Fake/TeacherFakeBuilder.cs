using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class TeacherFakeBuilder
{
    public static Teacher Build()
    {
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        return Teacher.Create(name, contactEmail);
    }

    public static (Teacher Teacher, Car Car) AddFakeCar(this Teacher teacher)
    {
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());
        var car = teacher.AddCar(name, type, Transmission.Automatic);

        return (teacher, car);
    }
}
