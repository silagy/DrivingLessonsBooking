using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Commands.ImportRoster;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class ImportRosterInteractorTest
{
    private IRosterCsvParser parser = null!;
    private IStudentRepository studentRepository = null!;
    private IRosterImportRepository rosterImportRepository = null!;
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ImportRosterInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;
    private RosterImport? persistedImport;

    [TestInitialize]
    public void Init()
    {
        parser = A.Fake<IRosterCsvParser>();
        studentRepository = A.Fake<IStudentRepository>();
        rosterImportRepository = A.Fake<IRosterImportRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ImportRosterInteractor(
            parser,
            studentRepository,
            rosterImportRepository,
            teacherRepository,
            carRepository,
            unitOfWork,
            TimeProvider.System);

        teacher = Teacher.Create(TeacherName.Of("משה לוי"), Email.Of("moshe@school.co.il"));
        car = Car.Create(CarName.Of("טויוטה 123"), CarType.Of("יאריס"), Transmission.Manual);
        persistedImport = null;

        A.CallTo(() => teacherRepository.FindActiveAsync())
            .Returns(new List<Teacher> { teacher });

        A.CallTo(() => carRepository.FindActiveAsync())
            .Returns(new List<Car> { car });

        A.CallTo(() => rosterImportRepository.Add(A<RosterImport>._))
            .Invokes(call => persistedImport = call.GetArgument<RosterImport>(0));

        StudentsAre();
    }

    [TestMethod]
    public async Task New_Row_Adds_Student()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(
                x => x.NationalId == NationalId.Of("123456782"))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Existing_Row_Updates_Student()
    {
        //given
        var student = ExistingStudent("123456782");
        StudentsAre(student);
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Updated.ShouldBe(1);
        student.Name.ShouldBe(StudentName.Of("דנה כהן"));
        student.Phone.ShouldBe(PhoneNumber.Of("0501234567"));
        A.CallTo(() => studentRepository.Add(A<Student>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Reappearing_Inactive_Student_Is_Reactivated()
    {
        //given
        var student = ExistingStudent("123456782");
        student.Deactivate();
        StudentsAre(student);
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        student.IsActive.ShouldBeTrue();
        response.Updated.ShouldBe(1);
    }

    [TestMethod]
    public async Task Absent_Student_Is_Deactivated()
    {
        //given
        var student = ExistingStudent("123456782");
        StudentsAre(student);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        student.IsActive.ShouldBeFalse();
        response.Deactivated.ShouldBe(1);
        persistedImport!.Entries.ShouldContain(x =>
            x.NationalId == NationalId.Of("123456782") && x.Outcome == RosterEntryOutcome.Deactivated);
    }

    [TestMethod]
    public async Task Absent_Inactive_Student_Is_Skipped()
    {
        //given
        var student = ExistingStudent("123456782");
        student.Deactivate();
        StudentsAre(student);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Deactivated.ShouldBe(0);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Invalid_National_Id_Is_Recorded_As_Failed_Row()
    {
        //given
        RowsAre(Row(2, "רות אברהם", "123456789"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2
            && x.StudentName == "רות אברהם"
            && x.Reason == RosterRowFailureReason.InvalidNationalId);
        A.CallTo(() => studentRepository.Add(A<Student>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unknown_Teacher_Is_Recorded_As_Failed_Row()
    {
        //given
        var row = new RosterCsvRow
        {
            RowNumber = 2,
            FullName = "דנה כהן",
            NationalId = "123456782",
            Phone = "0501234567",
            TeacherName = "מורה לא קיים",
            CarName = car.Name.Value
        };
        RowsAre(row);

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2 && x.Reason == RosterRowFailureReason.UnknownTeacher);
    }

    [TestMethod]
    public async Task Unknown_Car_Is_Recorded_As_Failed_Row()
    {
        //given
        var row = new RosterCsvRow
        {
            RowNumber = 2,
            FullName = "דנה כהן",
            NationalId = "123456782",
            Phone = "0501234567",
            TeacherName = teacher.Name.Value,
            CarName = "רכב לא קיים"
        };
        RowsAre(row);

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2 && x.Reason == RosterRowFailureReason.UnknownCar);
    }

    [TestMethod]
    public async Task Duplicate_National_Id_In_File_Keeps_First_Row()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"), Row(3, "דנה אחרת", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        response.Failed.ShouldBe(1);
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(
                x => x.Name == StudentName.Of("דנה כהן"))))
            .MustHaveHappenedOnceExactly();
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 3 && x.Reason == RosterRowFailureReason.DuplicateNationalId);
    }

    [TestMethod]
    public async Task Roster_Import_Record_Is_Persisted()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        A.CallTo(() => rosterImportRepository.Add(A<RosterImport>._))
            .MustHaveHappenedOnceExactly();
        persistedImport!.FileName.ShouldBe(RosterFileName.Of("roster.csv"));
        response.RosterImportId.ShouldBe(persistedImport.Id.Value);
    }

    [TestMethod]
    public async Task Commit_Happens_Once()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        await interactor.ExecuteAsync(Request());

        //then
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedOnceExactly();
    }

    private static ImportRosterRequest Request()
    {
        return new ImportRosterRequest("roster.csv", Stream.Null);
    }

    private void RowsAre(params RosterCsvRow[] rows)
    {
        A.CallTo(() => parser.Parse(A<Stream>._))
            .Returns(rows);
    }

    private void StudentsAre(params Student[] students)
    {
        A.CallTo(() => studentRepository.FindAllAsync())
            .Returns(students);
    }

    private RosterCsvRow Row(int rowNumber, string fullName, string nationalId)
    {
        return new RosterCsvRow
        {
            RowNumber = rowNumber,
            FullName = fullName,
            NationalId = nationalId,
            Phone = "0501234567",
            TeacherName = teacher.Name.Value,
            CarName = car.Name.Value
        };
    }

    private Student ExistingStudent(string nationalId)
    {
        var id = NationalId.Of(nationalId);
        var name = StudentName.Of("תלמיד קיים");
        var phone = PhoneNumber.Of("0500000000");

        return Student.Create(id, name, phone, teacher, car, null, null, null);
    }
}
