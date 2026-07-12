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

    [TestMethod]
    public void New_Teacher_Is_Not_Deleted()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //expected
        teacher.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Delete()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        teacher.Delete();

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        teacher.Delete();

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherDeleted>()
            .Where(x => x.TeacherId == teacher.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Delete__Must_Not_Be_Deleted()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        teacher.Delete();

        //when
        var act = () => teacher.Delete();

        //then
        Should.Throw<TeacherAlreadyDeletedException>(act);
    }

    [TestMethod]
    public void Remove_Car()
    {
        //given
        var (teacher, removed) = TeacherFakeBuilder.Build().AddFakeCar();
        var (_, survivor) = teacher.AddFakeCar();

        //when
        teacher.RemoveCar(removed);

        //then
        teacher.Cars.ShouldNotContain(x => x.Id == removed.Id);
        teacher.Cars.ShouldContain(x => x.Id == survivor.Id);
    }

    [TestMethod]
    public void Remove_Car__Add_Event()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        teacher.AddFakeCar();

        //when
        teacher.RemoveCar(car);

        //then
        teacher
            .UncommittedEvents
            .OfType<CarRemoved>()
            .Where(x => x.TeacherId == teacher.Id && x.CarId == car.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Remove_Car__Car_Must_Be_In_Teacher()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        teacher.AddFakeCar();
        var (_, foreignCar) = TeacherFakeBuilder.Build().AddFakeCar();

        //when
        var act = () => teacher.RemoveCar(foreignCar);

        //then
        Should.Throw<CarNotInTeacherException>(act);
    }

    [TestMethod]
    public void Remove_Car__Last_Car_Cannot_Be_Removed()
    {
        //given
        var (teacher, onlyCar) = TeacherFakeBuilder.Build().AddFakeCar();

        //when
        var act = () => teacher.RemoveCar(onlyCar);

        //then
        Should.Throw<LastCarCannotBeRemovedException>(act);
    }

    [TestMethod]
    public void Remove_Car__Last_Active_Car_Cannot_Be_Removed()
    {
        //given
        var (teacher, first) = TeacherFakeBuilder.Build().AddFakeCar();
        var (_, second) = teacher.AddFakeCar();
        teacher.RemoveCar(first);

        //when
        var act = () => teacher.RemoveCar(second);

        //then
        Should.Throw<LastCarCannotBeRemovedException>(act);
    }

    [TestMethod]
    public void Remove_Car__Removed_Car_Cannot_Be_Removed_Again()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        teacher.AddFakeCar();
        teacher.AddFakeCar();
        teacher.RemoveCar(car);

        //when
        var act = () => teacher.RemoveCar(car);

        //then
        Should.Throw<CarNotInTeacherException>(act);
    }

    [TestMethod]
    public void Change_Car_Details__Removed_Car_Cannot_Be_Changed()
    {
        //given
        var (teacher, car) = TeacherFakeBuilder.Build().AddFakeCar();
        teacher.AddFakeCar();
        teacher.RemoveCar(car);
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        var act = () => teacher.ChangeCarDetails(car, newName, newType, Transmission.Manual);

        //then
        Should.Throw<CarNotInTeacherException>(act);
    }
}
