using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class TeacherTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        //when
        var teacher = Teacher.Create(name, contactEmail);

        //then
        teacher.Name.ShouldBe(name);
        teacher.ContactEmail.ShouldBe(contactEmail);
        teacher.Cars.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        //when
        var teacher = Teacher.Create(name, contactEmail);

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherCreated>()
            .Where(x => x.TeacherId == teacher.Id && x.Name == name && x.ContactEmail == contactEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Add_Car()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        //when
        var car = teacher.AddCar(name, type, Transmission.Manual);

        //then
        teacher.Cars.ShouldContain(x => x.Id == car.Id);
        car.Name.ShouldBe(name);
        car.Type.ShouldBe(type);
        car.Transmission.ShouldBe(Transmission.Manual);
    }

    [TestMethod]
    public void Add_Car_Has_No_Maximum()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        teacher.AddFakeCar();
        teacher.AddFakeCar();

        //when
        var (_, third) = teacher.AddFakeCar();

        //then
        teacher.Cars.ShouldContain(x => x.Id == third.Id);
    }

    [TestMethod]
    public void Add_Car__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        //when
        var car = teacher.AddCar(name, type, Transmission.Automatic);

        //then
        teacher
            .UncommittedEvents
            .OfType<CarAdded>()
            .Where(x => x.TeacherId == teacher.Id && x.CarId == car.Id && x.Name == name)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var newName = TeacherName.Of(Faker.FakeString());
        var newEmail = Email.Of(Faker.FakeEmail());

        //when
        teacher.ChangeDetails(newName, newEmail);

        //then
        teacher.Name.ShouldBe(newName);
        teacher.ContactEmail.ShouldBe(newEmail);
    }

    [TestMethod]
    public void Change_Details__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var newName = TeacherName.Of(Faker.FakeString());
        var newEmail = Email.Of(Faker.FakeEmail());

        //when
        teacher.ChangeDetails(newName, newEmail);

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherDetailsChanged>()
            .Where(x => x.TeacherId == teacher.Id && x.Name == newName && x.ContactEmail == newEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details__Accepts_Unchanged_Values()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());
        var teacher = Teacher.Create(name, contactEmail);

        //when
        teacher.ChangeDetails(name, contactEmail);

        //then
        teacher.Name.ShouldBe(name);
        teacher.ContactEmail.ShouldBe(contactEmail);
    }

    [TestMethod]
    public void Change_Car_Details()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        teacher.ChangeCarDetails(car, newName, newType, Transmission.Manual);

        //then
        car.Name.ShouldBe(newName);
        car.Type.ShouldBe(newType);
        car.Transmission.ShouldBe(Transmission.Manual);
    }

    [TestMethod]
    public void Change_Car_Details__Add_Event()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        teacher.ChangeCarDetails(car, newName, newType, Transmission.Manual);

        //then
        teacher
            .UncommittedEvents
            .OfType<CarDetailsChanged>()
            .Where(x => x.TeacherId == teacher.Id && x.CarId == car.Id && x.Name == newName)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Car_Details__Car_Must_Be_In_Teacher()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var (_, foreignCar) = TeacherFakeBuilder.Build().AddFakeCar();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        var act = () => teacher.ChangeCarDetails(foreignCar, newName, newType, Transmission.Automatic);

        //then
        Should.Throw<CarNotInTeacherException>(act);
    }
}
