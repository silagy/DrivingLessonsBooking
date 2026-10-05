using DrivingLessons.Application.Commands.CreateStudent;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class CreateStudentInteractorTest
{
    private const string NationalIdValue = "123456782";
    private const string StudentNameValue = "שקד נבון";
    private const string PhoneValue = "050-3318842";
    private const string AddressValue = "הרימון 12, מודיעין";
    private const string LicenseTypeValue = "B";

    private IStudentRepository repository = null!;
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private CreateStudentInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;
    private Student? added;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new CreateStudentInteractor(repository, teacherRepository, carRepository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("יעל כרמי"), Email.Of("yael@school.example"));
        car = Car.Create(CarName.Of("פיקנטו אדומה"), CarType.Of("פיקנטו"), Transmission.Automatic);
        car.AssignTeacher(teacher);
        added = null;

        A.CallTo(() => repository.GetByNationalIdAsync(A<NationalId>._)).Returns((Student?)null);
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(car.Id)).Returns(car);
        A.CallTo(() => repository.Add(A<Student>._)).Invokes((Student student) => added = student);
    }

    [TestMethod]
    public async Task Creates_An_Active_Student_On_One_Of_The_Teachers_Cars()
    {
        //given
        var startDate = new DateOnly(2026, 9, 1);
        var request = new CreateStudentRequest(
            NationalIdValue,
            StudentNameValue,
            PhoneValue,
            teacher.Id.Value,
            car.Id.Value,
            AddressValue,
            startDate,
            LicenseTypeValue);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.NationalId.ShouldBe(NationalId.Of(NationalIdValue));
        added.Name.ShouldBe(StudentName.Of(StudentNameValue));
        added.Phone.ShouldBe(PhoneNumber.Of(PhoneValue));
        added.TeacherId.ShouldBe(teacher.Id);
        added.CarId.ShouldBe(car.Id);
        added.Address.ShouldBe(Address.Of(AddressValue));
        added.StartDate.ShouldBe(LessonsStartDate.Of(startDate));
        added.LicenseType.ShouldBe(LicenseType.Of(LicenseTypeValue));
        added.IsActive.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Creates_A_Student_Without_The_Optional_Details()
    {
        //given
        var request = new CreateStudentRequest(
            NationalIdValue,
            StudentNameValue,
            PhoneValue,
            teacher.Id.Value,
            car.Id.Value,
            null,
            null,
            null);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Address.ShouldBeNull();
        added.StartDate.ShouldBeNull();
        added.LicenseType.ShouldBeNull();
    }

    [TestMethod]
    public async Task Returns_The_New_Student_Id()
    {
        //given
        var request = RequestFor(NationalIdValue, teacher.Id.Value, car.Id.Value);

        //when
        var response = await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        response.Id.ShouldBe(added.Id.Value);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task National_Id_In_Use_Is_Rejected_Naming_The_Existing_Student(bool existingIsActive)
    {
        //given
        var existing = Student.Create(
            NationalId.Of(NationalIdValue),
            StudentName.Of("נועה מזרחי"),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            null,
            null,
            null);

        if (!existingIsActive)
        {
            existing.Deactivate();
        }

        A.CallTo(() => repository.GetByNationalIdAsync(NationalId.Of(NationalIdValue))).Returns(existing);
        var request = RequestFor(NationalIdValue, teacher.Id.Value, car.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        var refusal = await Should.ThrowAsync<StudentNationalIdAlreadyInUseException>(act);
        refusal.ExistingStudentName.ShouldBe(StudentName.Of("נועה מזרחי"));
        A.CallTo(() => repository.Add(A<Student>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task National_Id_Is_Checked_In_Its_Normalized_Form()
    {
        //given
        var existing = Student.Create(
            NationalId.Of("000000018"),
            StudentName.Of("נועה מזרחי"),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            null,
            null,
            null);
        A.CallTo(() => repository.GetByNationalIdAsync(NationalId.Of("000000018"))).Returns(existing);
        var request = RequestFor(" 18 ", teacher.Id.Value, car.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<StudentNationalIdAlreadyInUseException>(act);
    }

    [TestMethod]
    [DataRow("12a456789")]
    [DataRow("1234567890")]
    [DataRow("123456789")]
    public async Task Invalid_National_Id_Is_Rejected_Before_Any_Lookup(string nationalId)
    {
        //given
        var request = RequestFor(nationalId, teacher.Id.Value, car.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<DomainException>(act);
        A.CallTo(() => repository.GetByNationalIdAsync(A<NationalId>._)).MustNotHaveHappened();
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //given
        var request = RequestFor(NationalIdValue, Guid.NewGuid(), car.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => repository.Add(A<Student>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        var request = RequestFor(NationalIdValue, teacher.Id.Value, Guid.NewGuid());

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => repository.Add(A<Student>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Car_Not_Of_The_Teacher_Is_Rejected()
    {
        //given
        var otherCar = Car.Create(CarName.Of("מאזדה 3 אפורה"), CarType.Of("מאזדה"), Transmission.Manual);
        A.CallTo(() => carRepository.GetAsync(otherCar.Id)).Returns(otherCar);
        var request = RequestFor(NationalIdValue, teacher.Id.Value, otherCar.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        A.CallTo(() => repository.Add(A<Student>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static CreateStudentRequest RequestFor(string nationalId, Guid teacherId, Guid carId)
    {
        return new CreateStudentRequest(nationalId, StudentNameValue, PhoneValue, teacherId, carId, null, null, null);
    }
}
