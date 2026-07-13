using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class CarFakeBuilder
{
    public static Car Build()
    {
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        return Car.Create(name, type, Transmission.Automatic);
    }

    public static (Car Car, Teacher Teacher) AssignFakeTeacher(this Car car)
    {
        var teacher = TeacherFakeBuilder.Build();
        car.AssignTeacher(teacher);

        return (car, teacher);
    }
}
