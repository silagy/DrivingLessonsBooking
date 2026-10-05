using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Commands.ImportRoster;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
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
    private Teacher otherTeacher = null!;
    private Car otherCar = null!;
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
        car.AssignTeacher(teacher);
        persistedImport = null;

        otherTeacher = Teacher.Create(TeacherName.Of("רינה גל"), Email.Of("rina@school.co.il"));
        otherCar = Car.Create(CarName.Of("מאזדה 2"), CarType.Of("מאזדה"), Transmission.Automatic);
        otherCar.AssignTeacher(otherTeacher);

        A.CallTo(() => teacherRepository.FindActiveAsync())
            .Returns(new List<Teacher> { teacher, otherTeacher });

        A.CallTo(() => carRepository.FindActiveAsync())
            .Returns(new List<Car> { car, otherCar });

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
    public async Task Absent_Student_Stays_Active()
    {
        //given
        var student = ExistingStudent("123456782");
        StudentsAre(student);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        await interactor.ExecuteAsync(Request());

        //then
        student.IsActive.ShouldBeTrue();
        student.Name.ShouldBe(StudentName.Of("תלמיד קיים"));
        student.UncommittedEvents.OfType<StudentDeactivated>().ShouldBeEmpty();
        persistedImport!.Entries.ShouldNotContain(x => x.NationalId == NationalId.Of("123456782"));
    }

    [TestMethod]
    public async Task Absent_Inactive_Student_Stays_Inactive()
    {
        //given
        var student = ExistingStudent("123456782");
        student.Deactivate();
        StudentsAre(student);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        await interactor.ExecuteAsync(Request());

        //then
        student.IsActive.ShouldBeFalse();
        student.UncommittedEvents.OfType<StudentReactivated>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Import_Records_Only_Added_And_Updated_Entries()
    {
        //given
        var updated = ExistingStudent("123456782");
        var absent = ExistingStudent("987654324");
        StudentsAre(updated, absent);
        RowsAre(Row(2, "דנה כהן", "123456782"), Row(3, "יוסי מזרחי", "111111118"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        response.Updated.ShouldBe(1);
        response.Failed.ShouldBe(0);
        persistedImport!.Entries.Count.ShouldBe(2);
        persistedImport.Entries.ShouldContain(x =>
            x.NationalId == NationalId.Of("123456782") && x.Outcome == RosterEntryOutcome.Updated);
        persistedImport.Entries.ShouldContain(x =>
            x.NationalId == NationalId.Of("111111118") && x.Outcome == RosterEntryOutcome.Added);
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
    public async Task Car_Not_Of_Teacher_Is_Recorded_As_Failed_Row()
    {
        //given
        RowsAre(MismatchedRow(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(0);
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2
            && x.StudentName == "דנה כהן"
            && x.Reason == RosterRowFailureReason.CarNotAssignedToTeacher);
        A.CallTo(() => studentRepository.Add(A<Student>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Car_Not_Of_Teacher_Row_Does_Not_Stop_Other_Rows()
    {
        //given
        RowsAre(
            Row(2, "דנה כהן", "123456782"),
            MismatchedRow(3, "יוסי מזרחי", "987654324"),
            Row(4, "רות אברהם", "111111118"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(2);
        response.Failed.ShouldBe(1);
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(x => x.NationalId == NationalId.Of("123456782"))))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(x => x.NationalId == NationalId.Of("111111118"))))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Car_Not_Of_Teacher_Row_Leaves_Existing_Student_Unchanged()
    {
        //given
        var student = ExistingStudent("123456782");
        StudentsAre(student);
        RowsAre(MismatchedRow(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Updated.ShouldBe(0);
        response.Failed.ShouldBe(1);
        student.Name.ShouldBe(StudentName.Of("תלמיד קיים"));
        student.TeacherId.ShouldBe(teacher.Id);
        student.CarId.ShouldBe(car.Id);
        student.UncommittedEvents.OfType<StudentUpdatedFromRoster>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Car_Not_Of_Teacher_Row_Does_Not_Reactivate_Inactive_Student()
    {
        //given
        var student = ExistingStudent("123456782");
        student.Deactivate();
        StudentsAre(student);
        RowsAre(MismatchedRow(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        response.Updated.ShouldBe(0);
        student.IsActive.ShouldBeFalse();
        student.UncommittedEvents.OfType<StudentReactivated>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Shared_Car_Row_Is_Imported()
    {
        //given
        otherCar.AssignTeacher(teacher);
        RowsAre(MismatchedRow(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        response.Failed.ShouldBe(0);
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(
                x => x.TeacherId == teacher.Id && x.CarId == otherCar.Id)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Mismatched_Row_Still_Claims_Its_National_Id()
    {
        //given
        RowsAre(MismatchedRow(2, "דנה כהן", "123456782"), Row(3, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(0);
        response.Failed.ShouldBe(2);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2 && x.Reason == RosterRowFailureReason.CarNotAssignedToTeacher);
        persistedImport.Failures.ShouldContain(x =>
            x.RowNumber == 3 && x.Reason == RosterRowFailureReason.DuplicateNationalId);
    }

    [TestMethod]
    public async Task Existing_Violator_Absent_From_File_Does_Not_Break_Import()
    {
        //given
        var violator = ExistingViolator("123456782");
        StudentsAre(violator);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        response.Failed.ShouldBe(0);
        violator.IsActive.ShouldBeTrue();
        violator.UncommittedEvents.OfType<StudentUpdatedFromRoster>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Existing_Violator_Is_Updated_By_A_Consistent_Row()
    {
        //given
        var violator = ExistingViolator("123456782");
        StudentsAre(violator);
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Updated.ShouldBe(1);
        response.Failed.ShouldBe(0);
        violator.TeacherId.ShouldBe(teacher.Id);
        violator.CarId.ShouldBe(car.Id);
    }

    [TestMethod]
    [DataRow("06/09/2026")]
    [DataRow("6/9/2026")]
    [DataRow("2026-09-06")]
    public async Task Start_Date_Is_Parsed(string startDate)
    {
        //given
        RowsAre(RowWithStartDate(startDate));

        //when
        await interactor.ExecuteAsync(Request());

        //then
        var expected = LessonsStartDate.Of(new DateOnly(2026, 9, 6));
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(x => x.StartDate == expected)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow("2026/09/06")]
    [DataRow("09-06-2026")]
    [DataRow("not a date")]
    public async Task Invalid_Start_Date_Is_Recorded_As_Failed_Row(string startDate)
    {
        //given
        RowsAre(RowWithStartDate(startDate));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2 && x.Reason == RosterRowFailureReason.InvalidStartDate);
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

    private RosterCsvRow MismatchedRow(int rowNumber, string fullName, string nationalId)
    {
        return new RosterCsvRow
        {
            RowNumber = rowNumber,
            FullName = fullName,
            NationalId = nationalId,
            Phone = "0501234567",
            TeacherName = teacher.Name.Value,
            CarName = otherCar.Name.Value
        };
    }

    private RosterCsvRow RowWithStartDate(string startDate)
    {
        return new RosterCsvRow
        {
            RowNumber = 2,
            FullName = "דנה כהן",
            NationalId = "123456782",
            Phone = "0501234567",
            TeacherName = teacher.Name.Value,
            CarName = car.Name.Value,
            StartDate = startDate
        };
    }

    private Student ExistingStudent(string nationalId)
    {
        var id = NationalId.Of(nationalId);
        var name = StudentName.Of("תלמיד קיים");
        var phone = PhoneNumber.Of("0500000000");

        return Student.Create(id, name, phone, teacher, car, null, null, null);
    }

    private Student ExistingViolator(string nationalId)
    {
        var formerCar = Car.Create(CarName.Of("סקודה 7"), CarType.Of("אוקטביה"), Transmission.Manual);
        formerCar.AssignTeacher(teacher);
        var violator = Student.Create(
            NationalId.Of(nationalId),
            StudentName.Of("תלמיד ותיק"),
            PhoneNumber.Of("0500000000"),
            teacher,
            formerCar,
            null,
            null,
            null);
        formerCar.UnassignTeacher(teacher);

        return violator;
    }
}
