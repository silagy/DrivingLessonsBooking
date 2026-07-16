using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class StudentTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var address = Address.Of(Faker.FakeString());
        var startDate = LessonsStartDate.Of(Faker.FakeDate());
        var licenseType = LicenseType.Of(Faker.FakeString());

        //when
        var student = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);

        //then
        student.NationalId.ShouldBe(nationalId);
        student.Name.ShouldBe(name);
        student.Phone.ShouldBe(phone);
        student.TeacherId.ShouldBe(teacher.Id);
        student.CarId.ShouldBe(car.Id);
        student.Address.ShouldBe(address);
        student.StartDate.ShouldBe(startDate);
        student.LicenseType.ShouldBe(licenseType);
        student.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var address = Address.Of(Faker.FakeString());
        var startDate = LessonsStartDate.Of(Faker.FakeDate());
        var licenseType = LicenseType.Of(Faker.FakeString());

        //when
        var student = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);

        //then
        student
            .UncommittedEvents
            .OfType<StudentCreated>()
            .Where(x => x.StudentId == student.Id
                        && x.NationalId == nationalId
                        && x.Name == name
                        && x.TeacherId == teacher.Id
                        && x.CarId == car.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Update_From_Roster()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        var newAddress = Address.Of(Faker.FakeString());
        var newStartDate = LessonsStartDate.Of(Faker.FakeDate());
        var newLicenseType = LicenseType.Of(Faker.FakeString());

        //when
        student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, newAddress, newStartDate, newLicenseType);

        //then
        student.Name.ShouldBe(newName);
        student.Phone.ShouldBe(newPhone);
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.CarId.ShouldBe(newCar.Id);
        student.Address.ShouldBe(newAddress);
        student.StartDate.ShouldBe(newStartDate);
        student.LicenseType.ShouldBe(newLicenseType);
    }

    [TestMethod]
    public void Update_From_Roster__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        var newAddress = Address.Of(Faker.FakeString());
        var newStartDate = LessonsStartDate.Of(Faker.FakeDate());
        var newLicenseType = LicenseType.Of(Faker.FakeString());

        //when
        student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, newAddress, newStartDate, newLicenseType);

        //then
        student
            .UncommittedEvents
            .OfType<StudentUpdatedFromRoster>()
            .Where(x => x.StudentId == student.Id
                        && x.TeacherId == newTeacher.Id
                        && x.CarId == newCar.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Deactivate()
    {
        //given
        var student = new StudentFakeBuilder().Build();

        //when
        student.Deactivate();

        //then
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Deactivate__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();

        //when
        student.Deactivate();

        //then
        student
            .UncommittedEvents
            .OfType<StudentDeactivated>()
            .Where(x => x.StudentId == student.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Deactivate__Must_Not_Be_Inactive()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();

        //when
        var act = () => student.Deactivate();

        //then
        Should.Throw<StudentAlreadyDeactivatedException>(act);
    }

    [TestMethod]
    public void Reactivate()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();

        //when
        student.Reactivate();

        //then
        student.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void Reactivate__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();

        //when
        student.Reactivate();

        //then
        student
            .UncommittedEvents
            .OfType<StudentReactivated>()
            .Where(x => x.StudentId == student.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Reactivate__Must_Not_Be_Active()
    {
        //given
        var student = new StudentFakeBuilder().Build();

        //when
        var act = () => student.Reactivate();

        //then
        Should.Throw<StudentAlreadyActiveException>(act);
    }
}
