# Task 2 of 6: Create Student, refusing a national ID that is already used (backend)

> Part of [#93: Students screen: list, filter and add a Student by hand](README.md). Requires task 1 committed. Work on branch `93-students-screen`. Read README decisions 3 to 9 first.

**Files:**
- Modify: `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentNationalIdAlreadyInUseException.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentResponse.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (two `errors.*` keys)
- Test: `tests\DrivingLessons.Application.Test\Commands\CreateStudentInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`

**Interfaces:**
- Consumes: `Student.Create(NationalId, StudentName, PhoneNumber, Teacher, Car, Address?, LessonsStartDate?, LicenseType?)` (throws `StudentCarMustBeAssignedToTeacherException` from #92), `Student.Deactivate()`, `Car.AssignTeacher(Teacher)`, `NationalId.Of`, `StudentName.Of`, `PhoneNumber.Of`, `Address.Of`, `LessonsStartDate.Of`, `LicenseType.Of`, `TeacherId.Of`, `CarId.Of`, `ITeacherRepository.GetAsync(TeacherId)`, `ICarRepository.GetAsync(CarId)`, `IUnitOfWork.CommitAsync()` (`DrivingLessons.Application.Common`), `TeacherNotFoundException(TeacherId)`, `CarNotFoundException(CarId)`, the `Students_Stay_Administrator_Only` test from task 1.
- Produces (tasks 3 to 6 rely on these):
  - `Task<Student?> IStudentRepository.GetByNationalIdAsync(NationalId nationalId)`: any Student, active or Inactive.
  - `DrivingLessons.Domain.Exceptions.StudentNationalIdAlreadyInUseException(StudentName existingStudentName) : DomainException`, property `StudentName ExistingStudentName`. 409, `code` `studentNationalIdAlreadyInUse`, `params` `{ "name": "<existing Student's name>" }`.
  - `record CreateStudentRequest(string NationalId, string Name, string Phone, Guid TeacherId, Guid CarId, string? Address, DateOnly? StartDate, string? LicenseType)`; JSON camelCase, `startDate` as `"yyyy-MM-dd"`; `null` means absent.
  - `record CreateStudentResponse(Guid Id)`.
  - `POST api/students` → `StudentCommandController.CreateAsync`, Administrator-only: 201 `{ "id": ... }`; 404 `teacherNotFound` / `carNotFound`; 409 `studentNationalIdAlreadyInUse`, `nationalIdMustBeDigits`, `nationalIdMustBeAtMostNineDigits`, `nationalIdMustHaveValidCheckDigit`, `studentNameMustNotBeEmpty`, `phoneNumberMustBeValid`, `addressMustNotBeEmpty`, `licenseTypeMustNotBeEmpty`, `studentCarMustBeAssignedToTeacher`.
  - Translations `errors.studentNationalIdAlreadyInUse` (with `{{name}}`) and `errors.studentCarMustBeAssignedToTeacher` in both languages.

**Why:** AC "Create Student command", "national ID already exists is rejected (409, translated), checked in the interactor via the repository" and "the domain rejects a Car not assigned to the Teacher". The check matches Inactive Students too: the `students.national_id` column has a unique index, so a missed Inactive duplicate would fail at commit as a 500 instead of a translated 409 (Review Focus 1). The lookup takes the `NationalId` value object, so "18" and "000000018" are the same ID (Review Focus 2).

- [ ] **Step 1: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Commands\CreateStudentInteractorTest.cs`:

```csharp
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
```

(`000000018` passes the check digit; `123456789` does not; `12a456789` isn't digits; `1234567890` has ten digits.)

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateStudentInteractorTest"`
Expected: build error `The type or namespace name 'CreateStudent' does not exist in the namespace 'DrivingLessons.Application.Commands'`.

- [ ] **Step 3: Add the repository lookup and the exception**

Replace `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs` with:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyCollection<Student>> FindAllAsync();

    Task<Student?> GetActiveByNationalIdAsync(NationalId nationalId);

    Task<Student?> GetByNationalIdAsync(NationalId nationalId);

    void Add(Student student);
}
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`, add after `GetActiveByNationalIdAsync`:

```csharp
    public async Task<Student?> GetByNationalIdAsync(NationalId nationalId)
    {
        return await dbContext
                         .Students
                         .FirstOrDefaultAsync(x => x.NationalId == nationalId);
    }
```

Create `src\DrivingLessons.Domain\Exceptions\StudentNationalIdAlreadyInUseException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class StudentNationalIdAlreadyInUseException : DomainException
{
    public StudentName ExistingStudentName { get; }

    public StudentNationalIdAlreadyInUseException(StudentName existingStudentName)
        : base("Another Student already has this national ID.")
    {
        ExistingStudentName = existingStudentName;
    }
}
```

(The developer-facing `detail` carries no name: Student names are PII and never go to logs. The name reaches the Administrator only through `params`, step 8.)

- [ ] **Step 4: Add the request, the response and the interactor**

Create `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateStudent;

public record CreateStudentRequest(
    string NationalId,
    string Name,
    string Phone,
    Guid TeacherId,
    Guid CarId,
    string? Address,
    DateOnly? StartDate,
    string? LicenseType);
```

Create `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateStudent;

public record CreateStudentResponse(Guid Id);
```

Create `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateStudent;

public class CreateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateStudentInteractor(
        IStudentRepository repository,
        ITeacherRepository teacherRepository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateStudentResponse> ExecuteAsync(CreateStudentRequest request)
    {
        var nationalId = NationalId.Of(request.NationalId);
        var name = StudentName.Of(request.Name);
        var phone = PhoneNumber.Of(request.Phone);
        var address = AddressOf(request.Address);
        var startDate = StartDateOf(request.StartDate);
        var licenseType = LicenseTypeOf(request.LicenseType);

        await NationalIdMustBeFreeAsync(nationalId);

        var teacherId = TeacherId.Of(request.TeacherId);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var carId = CarId.Of(request.CarId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var student = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);

        repository.Add(student);

        await unitOfWork.CommitAsync();

        return new CreateStudentResponse(student.Id.Value);
    }

    private async Task NationalIdMustBeFreeAsync(NationalId nationalId)
    {
        var existing = await repository.GetByNationalIdAsync(nationalId);

        if (existing is not null)
        {
            throw new StudentNationalIdAlreadyInUseException(existing.Name);
        }
    }

    private static Address? AddressOf(string? value)
    {
        return value is null
            ? null
            : Address.Of(value);
    }

    private static LessonsStartDate? StartDateOf(DateOnly? value)
    {
        return value is null
            ? null
            : LessonsStartDate.Of(value.Value);
    }

    private static LicenseType? LicenseTypeOf(string? value)
    {
        return value is null
            ? null
            : LicenseType.Of(value);
    }
}
```

The interactor only orchestrates: the uniqueness check is a cross-Student question the aggregate can't answer (AC places it here), and "a Car not of the Teacher" stays the aggregate's rule (`Student.Create` throws).

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the using after `using DrivingLessons.Application.Commands.CreateCar;`:

```csharp
using DrivingLessons.Application.Commands.CreateStudent;
```

and register it right after `services.AddScoped<FindStudentsInteractor>();`:

```csharp
        services.AddScoped<CreateStudentInteractor>();
```

- [ ] **Step 5: Run the interactor tests**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateStudentInteractorTest"`
Expected: PASS, 12 tests (the two `[DataRow]` tests count once per row).

- [ ] **Step 6: Write the failing filter and policy tests**

Add to `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, after `Student_Car_Not_Of_Teacher_Is_A_Conflict_With_Its_Rule_As_Code`:

```csharp
    [TestMethod]
    public void National_Id_In_Use_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentNationalIdAlreadyInUseException(StudentName.Of("נועה מזרחי")));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentNationalIdAlreadyInUse");
    }
```

and after `Constraint_Length_Limit_Is_Named_In_Params`:

```csharp
    [TestMethod]
    public void National_Id_In_Use_Names_The_Existing_Student_In_Params()
    {
        //given
        var context = ContextFor(new StudentNationalIdAlreadyInUseException(StudentName.Of("נועה מזרחי")));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var parameters = ProblemOf(context).Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["name"] = "נועה מזרחי" });
    }

    [TestMethod]
    public void National_Id_In_Use_Keeps_The_Name_Out_Of_Its_Detail()
    {
        //given
        var context = ContextFor(new StudentNationalIdAlreadyInUseException(StudentName.Of("נועה מזרחי")));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        ProblemOf(context).Detail.ShouldBe("Another Student already has this national ID.");
    }
```

In `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`, add a third row to `Students_Stay_Administrator_Only`:

```csharp
    [TestMethod]
    [DataRow("StudentQueryController.FindAsync")]
    [DataRow("StudentQueryController.GetAsync")]
    [DataRow("StudentCommandController.CreateAsync")]
    public void Students_Stay_Administrator_Only(string endpoint)
```

- [ ] **Step 7: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest|FullyQualifiedName~ControllerAuthorizationTest"`
Expected: FAIL in `National_Id_In_Use_Names_The_Existing_Student_In_Params` (`KeyNotFoundException` for `params`) and in the `StudentCommandController.CreateAsync` row of `Students_Stay_Administrator_Only` (`KeyNotFoundException`). The two other new filter tests already pass (every `DomainException` is a 409 with its rule as `code`).

- [ ] **Step 8: Name the existing Student in `params` and add the endpoint**

In `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs`, in `ParamsOf`, add an arm before `_ => null`:

```csharp
            SlotConstraintMustNotExceedMaxLengthException tooLong => new Dictionary<string, string>
            {
                ["maxLength"] = tooLong.MaxLength.ToString(CultureInfo.InvariantCulture)
            },
            StudentNationalIdAlreadyInUseException inUse => new Dictionary<string, string>
            {
                ["name"] = inUse.ExistingStudentName.Value
            },
            _ => null
```

(The first arm is unchanged context; only the `StudentNationalIdAlreadyInUseException` arm is new.)

Create `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateStudent;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Student;

[ApiController]
[Route("api/students")]
[Tags("Students")]
public class StudentCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Add a student by hand, on one of their teacher's cars")]
    [ProducesResponseType(typeof(CreateStudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateStudentResponse>> CreateAsync(
        [FromServices] CreateStudentInteractor interactor,
        [FromBody] [Required] CreateStudentRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }
}
```

- [ ] **Step 9: Run them again**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest|FullyQualifiedName~ControllerAuthorizationTest"`
Expected: PASS, including `Every_Other_Endpoint_Is_Administrator_Only`.

- [ ] **Step 10: Translate the two Student rules**

Every rule code needs an `errors.{code}` key in both files (api-guidelines "Rule Codes"). `studentCarMustBeAssignedToTeacher` has had none since #92 (the Roster import never surfaced it); Add Student does.

In `client\public\i18n\he.json`, replace the line

```json
    "studentNameMustNotBeEmpty": "שם התלמיד לא יכול להיות ריק.",
```

with

```json
    "studentNameMustNotBeEmpty": "שם התלמיד לא יכול להיות ריק.",
    "studentCarMustBeAssignedToTeacher": "הרכב הזה לא משויך למורה הזה. יש לרענן ולבחור שוב.",
    "studentNationalIdAlreadyInUse": "כבר קיים תלמיד עם תעודת הזהות הזו: {{name}}.",
```

In `client\public\i18n\en.json`, replace the line

```json
    "studentNameMustNotBeEmpty": "The student's name can't be empty.",
```

with

```json
    "studentNameMustNotBeEmpty": "The student's name can't be empty.",
    "studentCarMustBeAssignedToTeacher": "This Car is not assigned to this Teacher. Refresh and choose again.",
    "studentNationalIdAlreadyInUse": "A Student with this national ID already exists: {{name}}.",
```

(Copy deck `err.carStale` and `err.nidExists`, with `{name}` written as Transloco's `{{name}}`.)

- [ ] **Step 11: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/translations.spec.ts
```

Expected: everything PASS, no pending model changes (no migration: the `national_id` unique index already exists).

- [ ] **Step 12: Smoke the API against Postgres (Review Focus 1 and 2)**

Use the compose Postgres, not `dl-postgres` (memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us93_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us93_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`). It migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us93_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Work in a folder in the scratchpad (`<scratchpad>` is this session's scratchpad directory). Hebrew on the Windows `curl` or `node` command line turns into `?????` (memory note), so the Hebrew payloads are written by a small `seed.js` and sent by relative `@file` paths, and the Hebrew name is compared through a file, never passed as an argument:

```bash
mkdir -p "<scratchpad>/smoke-93" && cd "<scratchpad>/smoke-93"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
auth() { echo "Authorization: Bearer $ADMIN"; }

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

cat > seed.js <<'EOF'
const fs = require('fs');
fs.writeFileSync('teacher-yael.json', JSON.stringify({ name: 'יעל כרמי', contactEmail: 'yael@school.example' }));
fs.writeFileSync('car-picanto.json', JSON.stringify({ name: 'פיקנטו אדומה', type: 'פיקנטו', transmission: 'automatic' }));
fs.writeFileSync('car-mazda.json', JSON.stringify({ name: 'מאזדה 3 אפורה', type: 'מאזדה', transmission: 'manual' }));
fs.writeFileSync('shaked-name.txt', 'שקד נבון');
EOF
node seed.js

YAEL=$(curl -s -X POST $API/api/teachers -H "$(auth)" -H "Content-Type: application/json" -d @teacher-yael.json | json id)
PICANTO=$(curl -s -X POST $API/api/cars -H "$(auth)" -H "Content-Type: application/json" -d @car-picanto.json | json id)
MAZDA=$(curl -s -X POST $API/api/cars -H "$(auth)" -H "Content-Type: application/json" -d @car-mazda.json | json id)
curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/$PICANTO/teachers/$YAEL" -H "$(auth)"

student() {
  node -e "
const fs = require('fs');
const [file, nationalId, teacherId, carId] = process.argv.slice(1);
fs.writeFileSync(file, JSON.stringify({ nationalId, name: fs.readFileSync('shaked-name.txt', 'utf8'), phone: '050-3318842', teacherId, carId, address: null, startDate: '2026-09-01', licenseType: 'B' }));
" "$@"
}
post() { curl -s -o body.json -w "%{http_code}" -X POST $API/api/students -H "$(auth)" -H "Content-Type: application/json" -d @"$1"; }

student add.json 123456782 $YAEL $PICANTO
echo "create $(post add.json)"; ID=$(json id < body.json)
curl -s -o get.json -w "get %{http_code}\n" "$API/api/students/$ID" -H "$(auth)"
node -pe "const s=JSON.parse(require('fs').readFileSync('get.json','utf8')); [s.nationalId, s.teacherId==='$YAEL', s.carTransmission, s.address, s.startDate, s.licenseType, s.isActive].join(' ')"
curl -s "$API/api/students/find" -H "$(auth)" | json "map(x => x.carTransmission).join(' ')"

echo "duplicate $(post add.json) $(json code < body.json) $(node -pe "JSON.parse(require('fs').readFileSync('body.json','utf8')).params.name === require('fs').readFileSync('shaked-name.txt','utf8')")"

student zero.json 000000018 $YAEL $PICANTO
echo "zero $(post zero.json)"
student short.json 18 $YAEL $PICANTO
echo "short $(post short.json) $(json code < body.json)"

docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us93_smoke -c "UPDATE students SET is_active = false WHERE national_id = '123456782'"
echo "inactive duplicate $(post add.json) $(json code < body.json)"

student invalid.json 123456789 $YAEL $PICANTO
echo "invalid $(post invalid.json) $(json code < body.json)"
student wrong-car.json 987654324 $YAEL $MAZDA
echo "wrong car $(post wrong-car.json) $(json code < body.json)"
student no-teacher.json 987654324 00000000-0000-0000-0000-000000000001 $PICANTO
echo "no teacher $(post no-teacher.json) $(json code < body.json)"
curl -s -o body.json -w "missing %{http_code}" "$API/api/students/00000000-0000-0000-0000-000000000001" -H "$(auth)"; echo " $(json code < body.json)"
```

Expected:
- `assign 204` (or `201`), `create 201`, `get 200`.
- The `get` line: `123456782 true automatic null 2026-09-01 B true`; the find line: `automatic`.
- `duplicate 409 studentNationalIdAlreadyInUse true` (the `params.name` is the existing Student's name).
- `zero 201`, then `short 409 studentNationalIdAlreadyInUse` ("18" is the same ID as `000000018`).
- `UPDATE 1`, then `inactive duplicate 409 studentNationalIdAlreadyInUse` (never `500`).
- `invalid 409 nationalIdMustHaveValidCheckDigit`, `wrong car 409 studentCarMustBeAssignedToTeacher`, `no teacher 404 teacherNotFound`, `missing 404 studentNotFound`.

If a route or field differs from what this step assumes, read the controller and adapt the script; don't change the code to fit the script. Stop the API (`TaskStop`) and drop the database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us93_smoke"
```

- [ ] **Step 13: Commit**

```bash
git add src/DrivingLessons.Domain/Repositories/IStudentRepository.cs src/DrivingLessons.Infrastructure/EntityFramework/Repositories/StudentRepository.cs src/DrivingLessons.Domain/Exceptions/StudentNationalIdAlreadyInUseException.cs src/DrivingLessons.Application/Commands/CreateStudent src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Presentation.Web/Controllers/Student/StudentCommandController.cs src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs client/public/i18n/he.json client/public/i18n/en.json tests/DrivingLessons.Application.Test/Commands/CreateStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs
git commit -m "feat(students): add a Student by hand, refusing a national ID already in use (#93)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
