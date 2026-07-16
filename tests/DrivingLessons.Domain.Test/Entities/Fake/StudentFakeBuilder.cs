using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public class StudentFakeBuilder
{
    private Teacher? teacher;
    private Car? car;

    public StudentFakeBuilder WithTeacher(Teacher teacher)
    {
        this.teacher = teacher;

        return this;
    }

    public StudentFakeBuilder WithCar(Car car)
    {
        this.car = car;

        return this;
    }

    public Student Build()
    {
        var resolvedTeacher = teacher ?? TeacherFakeBuilder.Build();
        var resolvedCar = car ?? CarFakeBuilder.Build();
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var address = Address.Of(Faker.FakeString());
        var startDate = LessonsStartDate.Of(Faker.FakeDate());
        var licenseType = LicenseType.Of(Faker.FakeString());

        return Student.Create(
            nationalId,
            name,
            phone,
            resolvedTeacher,
            resolvedCar,
            address,
            startDate,
            licenseType);
    }

    public Student BuildInactive()
    {
        var student = Build();
        student.Deactivate();

        return student;
    }
}
