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
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
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
        var (car, teacher) = CarFakeBuilder.Build().AssignFakeTeacher();
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
    public void Create_With_A_Shared_Car()
    {
        //given
        var (car, otherTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();
        var teacher = TeacherFakeBuilder.Build();
        car.AssignTeacher(teacher);
        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        var student = Student.Create(nationalId, name, phone, teacher, car, null, null, null);

        //then
        student.TeacherId.ShouldBe(teacher.Id);
        student.TeacherId.ShouldNotBe(otherTeacher.Id);
        student.CarId.ShouldBe(car.Id);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Create__Must_Be_Car_Of_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            car.AssignFakeTeacher();
        }

        var nationalId = Faker.FakeNationalId();
        var name = StudentName.Of(Faker.FakeString());
        var phone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        var act = () => Student.Create(nationalId, name, phone, teacher, car, null, null, null);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Update_From_Roster()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();
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
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();
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
    [DataRow(false)]
    [DataRow(true)]
    public void Update_From_Roster__Must_Be_Car_Of_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            newCar.AssignFakeTeacher();
        }

        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        var act = () => student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, null, null, null);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Update_From_Roster__Rejected_Car_Leaves_Student_Unchanged()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalName = student.Name;
        var originalPhone = student.Phone;
        var originalTeacherId = student.TeacherId;
        var originalCarId = student.CarId;
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(
            () => student.UpdateFromRoster(newName, newPhone, newTeacher, newCar, null, null, null));

        //then
        student.Name.ShouldBe(originalName);
        student.Phone.ShouldBe(originalPhone);
        student.TeacherId.ShouldBe(originalTeacherId);
        student.CarId.ShouldBe(originalCarId);
        student.UncommittedEvents.OfType<StudentUpdatedFromRoster>().ShouldBeEmpty();
    }

    [TestMethod]
    public void Change_Details()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalTeacherId = student.TeacherId;
        var originalCarId = student.CarId;
        var newNationalId = Faker.FakeNationalId();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());
        var newAddress = Address.Of(Faker.FakeString());
        var newStartDate = LessonsStartDate.Of(Faker.FakeDate());
        var newLicenseType = LicenseType.Of(Faker.FakeString());

        //when
        student.ChangeDetails(newNationalId, newName, newPhone, newAddress, newStartDate, newLicenseType);

        //then
        student.NationalId.ShouldBe(newNationalId);
        student.Name.ShouldBe(newName);
        student.Phone.ShouldBe(newPhone);
        student.Address.ShouldBe(newAddress);
        student.StartDate.ShouldBe(newStartDate);
        student.LicenseType.ShouldBe(newLicenseType);
        student.TeacherId.ShouldBe(originalTeacherId);
        student.CarId.ShouldBe(originalCarId);
        student.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void Change_Details__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newNationalId = Faker.FakeNationalId();
        var newName = StudentName.Of(Faker.FakeString());
        var newPhone = PhoneNumber.Of(Faker.FakePhoneNumber());

        //when
        student.ChangeDetails(newNationalId, newName, newPhone, null, null, null);

        //then
        student
            .UncommittedEvents
            .OfType<StudentDetailsChanged>()
            .Where(x => x.StudentId == student.Id
                        && x.NationalId == newNationalId
                        && x.Name == newName)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details_Clears_The_Optional_Details()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var nationalId = student.NationalId;
        var name = student.Name;
        var phone = student.Phone;

        //when
        student.ChangeDetails(nationalId, name, phone, null, null, null);

        //then
        student.Address.ShouldBeNull();
        student.StartDate.ShouldBeNull();
        student.LicenseType.ShouldBeNull();
    }

    [TestMethod]
    public void Change_Details_Of_An_Inactive_Student()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();
        var newName = StudentName.Of(Faker.FakeString());
        var nationalId = student.NationalId;
        var phone = student.Phone;

        //when
        student.ChangeDetails(nationalId, newName, phone, null, null, null);

        //then
        student.Name.ShouldBe(newName);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Change_Teacher()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        student.ChangeTeacher(newTeacher, newCar);

        //then
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.CarId.ShouldBe(newCar.Id);
    }

    [TestMethod]
    public void Change_Teacher__Add_Event()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        student.ChangeTeacher(newTeacher, newCar);

        //then
        student
            .UncommittedEvents
            .OfType<StudentTeacherChanged>()
            .Where(x => x.StudentId == student.Id
                        && x.TeacherId == newTeacher.Id
                        && x.CarId == newCar.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Teacher_Keeps_A_Car_The_New_Teacher_Also_Teaches_On()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var sharedCar = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(sharedCar).Build();
        var newTeacher = TeacherFakeBuilder.Build();
        sharedCar.AssignTeacher(newTeacher);

        //when
        student.ChangeTeacher(newTeacher, sharedCar);

        //then
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.CarId.ShouldBe(sharedCar.Id);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Teacher__Must_Not_Be_Current_Teacher(bool withAnotherCarOfTheTeacher)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();
        var chosenCar = car;

        if (withAnotherCarOfTheTeacher)
        {
            chosenCar = CarFakeBuilder.Build();
            chosenCar.AssignTeacher(teacher);
        }

        //when
        var act = () => student.ChangeTeacher(teacher, chosenCar);

        //then
        Should.Throw<StudentAlreadyWithTeacherException>(act);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Teacher__Must_Be_Car_Of_New_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            newCar.AssignFakeTeacher();
        }

        //when
        var act = () => student.ChangeTeacher(newTeacher, newCar);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Change_Teacher__Rejected_Change_Leaves_Student_Unchanged()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalTeacherId = student.TeacherId;
        var originalCarId = student.CarId;
        var newTeacher = TeacherFakeBuilder.Build();
        var newCar = CarFakeBuilder.Build();

        //when
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(() => student.ChangeTeacher(newTeacher, newCar));

        //then
        student.TeacherId.ShouldBe(originalTeacherId);
        student.CarId.ShouldBe(originalCarId);
        student.UncommittedEvents.OfType<StudentTeacherChanged>().ShouldBeEmpty();
    }

    [TestMethod]
    public void Change_Teacher__Current_Teacher_Is_Refused_Before_The_Car_Rule()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var carOfAnotherTeacher = CarFakeBuilder.Build();
        carOfAnotherTeacher.AssignFakeTeacher();

        //when
        var act = () => student.ChangeTeacher(teacher, carOfAnotherTeacher);

        //then
        Should.Throw<StudentAlreadyWithTeacherException>(act);
    }

    [TestMethod]
    public void Change_Teacher_Of_An_Inactive_Student()
    {
        //given
        var student = new StudentFakeBuilder().BuildInactive();
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        student.ChangeTeacher(newTeacher, newCar);

        //then
        student.TeacherId.ShouldBe(newTeacher.Id);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void Change_Teacher_Leaves_Existing_Submissions_With_Their_Publication()
    {
        //given
        var scenario = SubmissionFakeBuilder.BuildScenario();
        var submission = SubmissionFakeBuilder.Build(scenario);
        var publicationId = submission.PublicationId;
        var weekScheduleId = submission.WeekScheduleId;
        var (newCar, newTeacher) = CarFakeBuilder.Build().AssignFakeTeacher();

        //when
        scenario.Student.ChangeTeacher(newTeacher, newCar);

        //then
        submission.StudentId.ShouldBe(scenario.Student.Id);
        submission.PublicationId.ShouldBe(publicationId);
        submission.WeekScheduleId.ShouldBe(weekScheduleId);
    }

    [TestMethod]
    public void Change_Car()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student.CarId.ShouldBe(newCar.Id);
        student.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void Change_Car__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student
            .UncommittedEvents
            .OfType<StudentCarChanged>()
            .Where(x => x.StudentId == student.Id && x.CarId == newCar.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Car_Fixes_A_Car_That_Is_Not_The_Teachers()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var oldCar = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(oldCar).Build();
        oldCar.UnassignTeacher(teacher);
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student.CarId.ShouldBe(newCar.Id);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Car__Must_Not_Be_Current_Car(bool inactive)
    {
        //given
        var car = CarFakeBuilder.Build();
        var builder = new StudentFakeBuilder().WithCar(car);
        var student = inactive
            ? builder.BuildInactive()
            : builder.Build();

        //when
        var act = () => student.ChangeCar(car);

        //then
        Should.Throw<StudentAlreadyOnCarException>(act);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Change_Car__Must_Be_Car_Of_Teacher(bool carAssignedToAnotherTeacher)
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var newCar = CarFakeBuilder.Build();

        if (carAssignedToAnotherTeacher)
        {
            newCar.AssignFakeTeacher();
        }

        //when
        var act = () => student.ChangeCar(newCar);

        //then
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(act);
    }

    [TestMethod]
    public void Change_Car__Rejected_Change_Leaves_Student_Unchanged()
    {
        //given
        var student = new StudentFakeBuilder().Build();
        var originalCarId = student.CarId;
        var newCar = CarFakeBuilder.Build();

        //when
        Should.Throw<StudentCarMustBeAssignedToTeacherException>(() => student.ChangeCar(newCar));

        //then
        student.CarId.ShouldBe(originalCarId);
        student.UncommittedEvents.OfType<StudentCarChanged>().ShouldBeEmpty();
    }

    [TestMethod]
    public void Change_Car__Current_Car_Is_Refused_Before_The_Teacher_Rule()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var car = CarFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).WithCar(car).Build();
        car.UnassignTeacher(teacher);

        //when
        var act = () => student.ChangeCar(car);

        //then
        Should.Throw<StudentAlreadyOnCarException>(act);
    }

    [TestMethod]
    public void Change_Car_Of_An_Inactive_Student()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).BuildInactive();
        var newCar = CarFakeBuilder.Build();
        newCar.AssignTeacher(teacher);

        //when
        student.ChangeCar(newCar);

        //then
        student.CarId.ShouldBe(newCar.Id);
        student.IsActive.ShouldBeFalse();
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
