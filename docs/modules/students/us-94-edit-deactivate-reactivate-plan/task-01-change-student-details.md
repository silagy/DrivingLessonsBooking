# Task 1 of 7: Change a Student's details, refusing a national ID another Student uses (backend)

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Work on branch `94-edit-deactivate-reactivate-students`. Read README decisions 1 to 4, 6 and 7 first.

**Files:**
- Create: `src\DrivingLessons.Domain\Events\StudentDetailsChanged.cs`
- Modify: `src\DrivingLessons.Domain\Entities\Student.cs`
- Modify: `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeStudentDetails\ChangeStudentDetailsRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeStudentDetails\ChangeStudentDetailsInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\ChangeStudentDetailsInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`

**Interfaces:**
- Consumes: `NationalId.Of`, `StudentName.Of`, `PhoneNumber.Of`, `Address.Of`, `LessonsStartDate.Of`, `LicenseType.Of`, `StudentId.Of`, `IStudentRepository.GetByNationalIdAsync(NationalId)` (#93, any state), `StudentNationalIdAlreadyInUseException(StudentName existingStudentName)` (#93, 409 `studentNationalIdAlreadyInUse`, `params.name`), `StudentNotFoundException(StudentId)` (404 `studentNotFound`), `IUnitOfWork.CommitAsync()` (`DrivingLessons.Application.Common`), test helpers `Faker.FakeNationalId()`, `Faker.FakeString()`, `Faker.FakePhoneNumber()`, `Faker.FakeDate()`, `StudentFakeBuilder.Build()` / `BuildInactive()`.
- Produces (tasks 2 to 7 rely on these):
  - `void Student.ChangeDetails(NationalId nationalId, StudentName name, PhoneNumber phone, Address? address, LessonsStartDate? startDate, LicenseType? licenseType)`, no state guard, emits `StudentDetailsChanged`.
  - `record StudentDetailsChanged(StudentId StudentId, NationalId NationalId, StudentName Name) : IDomainEvent`.
  - `Task<Student?> IStudentRepository.GetAsync(StudentId id)`.
  - `record ChangeStudentDetailsRequest(string NationalId, string Name, string Phone, string? Address, DateOnly? StartDate, string? LicenseType)`; JSON camelCase, `startDate` as `"yyyy-MM-dd"`, `null` = absent.
  - `PUT api/students/{id}/details` → `StudentCommandController.ChangeDetailsAsync`, Administrator-only: 204; 404 `studentNotFound`; 409 `studentNationalIdAlreadyInUse` (`params.name` = the other Student), `nationalIdMustBeDigits`, `nationalIdMustBeAtMostNineDigits`, `nationalIdMustHaveValidCheckDigit`, `studentNameMustNotBeEmpty`, `phoneNumberMustBeValid`, `addressMustNotBeEmpty`, `licenseTypeMustNotBeEmpty`.

**Why:** AC "`Student.ChangeDetails` (name, phone, address, start date, license type, national ID) emits a new event (`StudentTest`)" and "An edited national ID that another Student already uses is rejected (409, translated) - interactor test". The check must skip the Student's own ID (Review Focus 1) and clearing an optional detail must clear it (Review Focus 2).

- [ ] **Step 1: Write the failing domain tests**

In `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`, add these four tests right after `Update_From_Roster__Rejected_Car_Leaves_Student_Unchanged` (before `Deactivate`):

```csharp
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
```

(No `Change_Details__Must_Be_*` test: the method has no guard, README decision 2. `StudentFakeBuilder` always sets address, start date and license type, so the clearing test starts from non-null values.)

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~StudentTest"`
Expected: build error `'Student' does not contain a definition for 'ChangeDetails'` and `The type or namespace name 'StudentDetailsChanged' could not be found`.

- [ ] **Step 3: Add the event and `ChangeDetails`**

Create `src\DrivingLessons.Domain\Events\StudentDetailsChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentDetailsChanged(StudentId StudentId, NationalId NationalId, StudentName Name) : IDomainEvent;
```

In `src\DrivingLessons.Domain\Entities\Student.cs`, add after the `UpdateFromRoster` method (before `Deactivate`):

```csharp
    public void ChangeDetails(
        NationalId nationalId,
        StudentName name,
        PhoneNumber phone,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        NationalId = nationalId;
        Name = name;
        Phone = phone;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;

        AddEvent(new StudentDetailsChanged(Id, nationalId, name));
    }
```

- [ ] **Step 4: Run the domain tests**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~StudentTest"`
Expected: PASS (the four new tests and every existing `StudentTest`).

- [ ] **Step 5: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Commands\ChangeStudentDetailsInteractorTest.cs`:

```csharp
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
```

(`205374184` and `000000018` pass the check digit, `123456789` does not, `12a456789` isn't digits, `1234567890` has ten digits.)

- [ ] **Step 6: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ChangeStudentDetailsInteractorTest"`
Expected: build error `The type or namespace name 'ChangeStudentDetails' does not exist in the namespace 'DrivingLessons.Application.Commands'`.

- [ ] **Step 7: Add the repository lookup, the request and the interactor**

Replace `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs` with:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyCollection<Student>> FindAllAsync();

    Task<Student?> GetAsync(StudentId id);

    Task<Student?> GetActiveByNationalIdAsync(NationalId nationalId);

    Task<Student?> GetByNationalIdAsync(NationalId nationalId);

    void Add(Student student);
}
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`, add after `FindAllAsync`:

```csharp
    public async Task<Student?> GetAsync(StudentId id)
    {
        return await dbContext
                         .Students
                         .FirstOrDefaultAsync(x => x.Id == id);
    }
```

Create `src\DrivingLessons.Application\Commands\ChangeStudentDetails\ChangeStudentDetailsRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeStudentDetails;

public record ChangeStudentDetailsRequest(
    string NationalId,
    string Name,
    string Phone,
    string? Address,
    DateOnly? StartDate,
    string? LicenseType);
```

Create `src\DrivingLessons.Application\Commands\ChangeStudentDetails\ChangeStudentDetailsInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeStudentDetails;

public class ChangeStudentDetailsInteractor
{
    private readonly IStudentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeStudentDetailsInteractor(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeStudentDetailsRequest request)
    {
        var nationalId = NationalId.Of(request.NationalId);
        var name = StudentName.Of(request.Name);
        var phone = PhoneNumber.Of(request.Phone);
        var address = AddressOf(request.Address);
        var startDate = StartDateOf(request.StartDate);
        var licenseType = LicenseTypeOf(request.LicenseType);
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        await NationalIdMustBeFreeAsync(student, nationalId);

        student.ChangeDetails(nationalId, name, phone, address, startDate, licenseType);

        await unitOfWork.CommitAsync();
    }

    private async Task NationalIdMustBeFreeAsync(Student student, NationalId nationalId)
    {
        if (nationalId == student.NationalId)
        {
            return;
        }

        var other = await repository.GetByNationalIdAsync(nationalId);

        if (other is not null)
        {
            throw new StudentNationalIdAlreadyInUseException(other.Name);
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

(The three optional-value helpers mirror `CreateStudentInteractor`'s; they are private one-liners, so they're not extracted for two callers.)

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the using after `using DrivingLessons.Application.Commands.ChangeMyPassword;` (alphabetical):

```csharp
using DrivingLessons.Application.Commands.ChangeStudentDetails;
```

Register it right after `services.AddScoped<CreateStudentInteractor>();`:

```csharp
        services.AddScoped<ChangeStudentDetailsInteractor>();
```

- [ ] **Step 8: Run the interactor tests**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ChangeStudentDetailsInteractorTest"`
Expected: PASS, 11 tests (the two `[DataRow]` tests count once per row).

- [ ] **Step 9: Pin the endpoint as Administrator-only (failing first)**

In `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`, add a row to `Students_Stay_Administrator_Only`:

```csharp
    [TestMethod]
    [DataRow("StudentQueryController.FindAsync")]
    [DataRow("StudentQueryController.GetAsync")]
    [DataRow("StudentCommandController.CreateAsync")]
    [DataRow("StudentCommandController.ChangeDetailsAsync")]
    public void Students_Stay_Administrator_Only(string endpoint)
```

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ControllerAuthorizationTest"`
Expected: FAIL in the `StudentCommandController.ChangeDetailsAsync` row (`KeyNotFoundException`).

- [ ] **Step 10: Add the endpoint**

Replace `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs` with:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ChangeStudentDetails;
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

    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the student's national ID, name, phone, address, start date and license type")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeStudentDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeStudentDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }
}
```

- [ ] **Step 11: Run every backend suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: everything PASS (including `Every_Other_Endpoint_Is_Administrator_Only` and the `SourceTextTest` source checks); no pending model changes. The API smoke for this endpoint runs in task 2 step 9, together with Deactivate and Reactivate.

- [ ] **Step 12: Commit**

```bash
git add src/DrivingLessons.Domain/Events/StudentDetailsChanged.cs src/DrivingLessons.Domain/Entities/Student.cs src/DrivingLessons.Domain/Repositories/IStudentRepository.cs src/DrivingLessons.Infrastructure/EntityFramework/Repositories/StudentRepository.cs src/DrivingLessons.Application/Commands/ChangeStudentDetails src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Presentation.Web/Controllers/Student/StudentCommandController.cs tests/DrivingLessons.Domain.Test/Entities/StudentTest.cs tests/DrivingLessons.Application.Test/Commands/ChangeStudentDetailsInteractorTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs
git commit -m "feat(students): change a Student's details, refusing a national ID another Student uses (#94)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
