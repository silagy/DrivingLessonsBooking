using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class CarTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        //when
        var car = Car.Create(name, type, Transmission.Manual);

        //then
        car.Name.ShouldBe(name);
        car.Type.ShouldBe(type);
        car.Transmission.ShouldBe(Transmission.Manual);
        car.TeacherAssignments.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());

        //when
        var car = Car.Create(name, type, Transmission.Automatic);

        //then
        car
            .UncommittedEvents
            .OfType<CarCreated>()
            .Where(x => x.CarId == car.Id && x.Name == name && x.Type == type && x.Transmission == Transmission.Automatic)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void New_Car_Is_Not_Deleted()
    {
        //given
        var car = CarFakeBuilder.Build();

        //expected
        car.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Change_Details()
    {
        //given
        var car = CarFakeBuilder.Build();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        car.ChangeDetails(newName, newType, Transmission.Manual);

        //then
        car.Name.ShouldBe(newName);
        car.Type.ShouldBe(newType);
        car.Transmission.ShouldBe(Transmission.Manual);
    }

    [TestMethod]
    public void Change_Details__Add_Event()
    {
        //given
        var car = CarFakeBuilder.Build();
        var newName = CarName.Of(Faker.FakeString());
        var newType = CarType.Of(Faker.FakeString());

        //when
        car.ChangeDetails(newName, newType, Transmission.Manual);

        //then
        car
            .UncommittedEvents
            .OfType<CarDetailsChanged>()
            .Where(x => x.CarId == car.Id && x.Name == newName && x.Type == newType && x.Transmission == Transmission.Manual)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details__Accepts_Unchanged_Values()
    {
        //given
        var name = CarName.Of(Faker.FakeString());
        var type = CarType.Of(Faker.FakeString());
        var car = Car.Create(name, type, Transmission.Automatic);

        //when
        car.ChangeDetails(name, type, Transmission.Automatic);

        //then
        car.Name.ShouldBe(name);
        car.Type.ShouldBe(type);
        car.Transmission.ShouldBe(Transmission.Automatic);
    }

    [TestMethod]
    public void Assign_Teacher()
    {
        //given
        var car = CarFakeBuilder.Build();
        var teacher = TeacherFakeBuilder.Build();

        //when
        car.AssignTeacher(teacher);

        //then
        car.TeacherAssignments.ShouldContain(x => x.TeacherId == teacher.Id);
    }

    [TestMethod]
    public void Assign_Teacher__Add_Event()
    {
        //given
        var car = CarFakeBuilder.Build();
        var teacher = TeacherFakeBuilder.Build();

        //when
        car.AssignTeacher(teacher);

        //then
        car
            .UncommittedEvents
            .OfType<CarAssignedToTeacher>()
            .Where(x => x.CarId == car.Id && x.TeacherId == teacher.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Assign_Teacher__Supports_Multiple_Teachers()
    {
        //given
        var (car, first) = CarFakeBuilder.Build().AssignFakeTeacher();
        var second = TeacherFakeBuilder.Build();

        //when
        car.AssignTeacher(second);

        //then
        car.TeacherAssignments.ShouldContain(x => x.TeacherId == first.Id);
        car.TeacherAssignments.ShouldContain(x => x.TeacherId == second.Id);
    }

    [TestMethod]
    public void Assign_Teacher__Teacher_Must_Not_Be_Assigned()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        var act = () => car.AssignTeacher(teacher);

        //then
        Should.Throw<TeacherAlreadyAssignedToCarException>(act);
    }

    [TestMethod]
    public void Unassign_Teacher()
    {
        //given
        var (car, removed) = CarFakeBuilder.Build().AssignFakeTeacher();
        var survivor = TeacherFakeBuilder.Build();
        car.AssignTeacher(survivor);

        //when
        car.UnassignTeacher(removed);

        //then
        car.TeacherAssignments.ShouldNotContain(x => x.TeacherId == removed.Id);
        car.TeacherAssignments.ShouldContain(x => x.TeacherId == survivor.Id);
    }

    [TestMethod]
    public void Unassign_Teacher__Add_Event()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        car.UnassignTeacher(teacher);

        //then
        car
            .UncommittedEvents
            .OfType<CarUnassignedFromTeacher>()
            .Where(x => x.CarId == car.Id && x.TeacherId == teacher.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Unassign_Teacher__Teacher_Must_Be_Assigned()
    {
        //given
        var car = CarFakeBuilder.Build();
        var teacher = TeacherFakeBuilder.Build();

        //when
        var act = () => car.UnassignTeacher(teacher);

        //then
        Should.Throw<TeacherNotAssignedToCarException>(act);
    }

    [TestMethod]
    public void Unassign_Teacher__Unassigned_Teacher_Cannot_Be_Unassigned_Again()
    {
        //given
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
        car.UnassignTeacher(teacher);

        //when
        var act = () => car.UnassignTeacher(teacher);

        //then
        Should.Throw<TeacherNotAssignedToCarException>(act);
    }

    [TestMethod]
    public void Delete()
    {
        //given
        var car = CarFakeBuilder.Build();

        //when
        car.Delete();

        //then
        car.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Add_Event()
    {
        //given
        var car = CarFakeBuilder.Build();

        //when
        car.Delete();

        //then
        car
            .UncommittedEvents
            .OfType<CarDeleted>()
            .Where(x => x.CarId == car.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Delete__Must_Not_Be_Deleted()
    {
        //given
        var car = CarFakeBuilder.Build();
        car.Delete();

        //when
        var act = () => car.Delete();

        //then
        Should.Throw<CarAlreadyDeletedException>(act);
    }
}
