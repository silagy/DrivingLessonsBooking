using DrivingLessons.Application.Commands.ChangeStudentDetails;
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
public class ChangeStudentDetailsInteractorTest
{
    private const string NoaNationalId = "205374184";
    private const string NoaName = "נועה מזרחי";
    private const string NewPhone = "050-1234568";
    private const string NewAddress = "הרימון 12, מודיעין";
    private const string NewLicenseType = "B";
    private const string OtherNationalId = "000000018";

    private IStudentRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeStudentDetailsInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;
    private Student noa = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeStudentDetailsInteractor(repository, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        car = Car.Create(CarName.Of("קורולה לבנה"), CarType.Of("קורולה"), Transmission.Automatic);
        car.AssignTeacher(teacher);
        noa = StudentOf(NoaNationalId, NoaName);

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(noa.Id)).Returns(noa);
        A.CallTo(() => repository.GetByNationalIdAsync(A<NationalId>._)).Returns((Student?)null);
    }

    [TestMethod]
    public async Task Changes_Every_Detail()
    {
        //given
        var startDate = new DateOnly(2026, 9, 1);
        var request = new ChangeStudentDetailsRequest(
            OtherNationalId,
            "נועה מזרחי-לוי",
            NewPhone,
            NewAddress,
            startDate,
            NewLicenseType);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.NationalId.ShouldBe(NationalId.Of(OtherNationalId));
        noa.Name.ShouldBe(StudentName.Of("נועה מזרחי-לוי"));
        noa.Phone.ShouldBe(PhoneNumber.Of(NewPhone));
        noa.Address.ShouldBe(Address.Of(NewAddress));
        noa.StartDate.ShouldBe(LessonsStartDate.Of(startDate));
        noa.LicenseType.ShouldBe(LicenseType.Of(NewLicenseType));
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Clears_The_Optional_Details_Left_Blank()
    {
        //given
        var request = new ChangeStudentDetailsRequest(NoaNationalId, NoaName, NewPhone, null, null, null);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.Address.ShouldBeNull();
        noa.StartDate.ShouldBeNull();
        noa.LicenseType.ShouldBeNull();
    }

    [TestMethod]
    public async Task Keeps_Its_Own_National_Id_Without_A_Lookup()
    {
        //given
        var request = new ChangeStudentDetailsRequest(NoaNationalId, NoaName, NewPhone, null, null, null);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.Phone.ShouldBe(PhoneNumber.Of(NewPhone));
        A.CallTo(() => repository.GetByNationalIdAsync(A<NationalId>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Keeps_Its_Own_National_Id_Written_Without_Leading_Zeros()
    {
        //given
        var shortId = StudentOf(OtherNationalId, "דנה ששון");
        A.CallTo(() => repository.GetAsync(shortId.Id)).Returns(shortId);
        A.CallTo(() => repository.GetByNationalIdAsync(NationalId.Of(OtherNationalId))).Returns(shortId);
        var request = new ChangeStudentDetailsRequest(" 18 ", "דנה ששון", NewPhone, null, null, null);

        //when
        await interactor.ExecuteAsync(shortId.Id.Value, request);

        //then
        shortId.NationalId.ShouldBe(NationalId.Of(OtherNationalId));
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task National_Id_Used_By_Another_Student_Is_Rejected_Naming_Them(bool otherIsActive)
    {
        //given
        var omer = StudentOf(OtherNationalId, "עומר שלו");

        if (!otherIsActive)
        {
            omer.Deactivate();
        }

        A.CallTo(() => repository.GetByNationalIdAsync(NationalId.Of(OtherNationalId))).Returns(omer);
        var request = new ChangeStudentDetailsRequest(OtherNationalId, NoaName, NewPhone, null, null, null);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        var refusal = await Should.ThrowAsync<StudentNationalIdAlreadyInUseException>(act);
        refusal.ExistingStudentName.ShouldBe(StudentName.Of("עומר שלו"));
        noa.NationalId.ShouldBe(NationalId.Of(NoaNationalId));
        noa.Phone.ShouldNotBe(PhoneNumber.Of(NewPhone));
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("12a456789")]
    [DataRow("1234567890")]
    [DataRow("123456789")]
    public async Task Invalid_National_Id_Is_Rejected_Before_Any_Lookup(string nationalId)
    {
        //given
        var request = new ChangeStudentDetailsRequest(nationalId, NoaName, NewPhone, null, null, null);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<DomainException>(act);
        A.CallTo(() => repository.GetAsync(A<StudentId>._)).MustNotHaveHappened();
        A.CallTo(() => repository.GetByNationalIdAsync(A<NationalId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentDetailsRequest(NoaNationalId, NoaName, NewPhone, null, null, null);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Changes_The_Details_Of_An_Inactive_Student()
    {
        //given
        noa.Deactivate();
        var request = new ChangeStudentDetailsRequest(NoaNationalId, NoaName, NewPhone, null, null, null);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.Phone.ShouldBe(PhoneNumber.Of(NewPhone));
        noa.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    private Student StudentOf(string nationalId, string name)
    {
        return Student.Create(
            NationalId.Of(nationalId),
            StudentName.Of(name),
            PhoneNumber.Of("050-1234567"),
            teacher,
            car,
            Address.Of("הגפן 3, רעננה"),
            LessonsStartDate.Of(new DateOnly(2026, 8, 2)),
            LicenseType.Of("B"));
    }
}
