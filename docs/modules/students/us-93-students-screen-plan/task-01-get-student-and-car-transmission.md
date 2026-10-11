# Task 1 of 6: Get Student endpoint and the Car's transmission in find-students (backend)

> Part of [#93: Students screen: list, filter and add a Student by hand](README.md). Requires the plan commit. Work on branch `93-students-screen`. Read README decisions 1, 2 and 8 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\GetStudent\GetStudentResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\GetStudent\GetStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\IStudentQueries.cs`
- Modify: `src\DrivingLessons.Application\Common\Exceptions\StudentNotFoundException.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentQueryController.cs`
- Test: `tests\DrivingLessons.Application.Test\Queries\GetStudentInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`

**Interfaces:**
- Consumes: `IStudentQueries.FindAsync(Guid? teacherId)` (existing), `StudentId.Of(Guid)`, `Transmission` (`Automatic = 10`, `Manual = 20`, serialized camelCase), `StudentNotFoundException()` (existing, used by `IdentifyStudentInteractor`), `AuthorizationPolicies.Administrator`, the private helper `Endpoints()` in `ControllerAuthorizationTest`.
- Produces (tasks 2 to 6 rely on these):
  - `ItemForFindStudentsResponse.CarTransmission : Transmission`, JSON `"carTransmission": "automatic" | "manual"`. Every other property unchanged.
  - `DrivingLessons.Application.Queries.GetStudent.GetStudentResponse` with `Guid Id`, `string NationalId`, `string Name`, `string Phone`, `Guid TeacherId`, `string TeacherName`, `Guid CarId`, `string CarName`, `Transmission CarTransmission`, `string? Address`, `DateOnly? StartDate`, `string? LicenseType`, `bool IsActive`.
  - `Task<GetStudentResponse?> IStudentQueries.GetAsync(Guid id)`.
  - `GetStudentInteractor.ExecuteAsync(Guid id) : Task<GetStudentResponse>`, throws `StudentNotFoundException` when the query returns null.
  - `public StudentNotFoundException(StudentId id)` (new overload; same code `studentNotFound`).
  - `GET api/students/{id:guid}` → `StudentQueryController.GetAsync`, Administrator-only (fallback policy), 200 or 404.

**Why:** AC "Get-Student query endpoint" and the Car cell of the design (frame 2a shows the Car's transmission as a tag next to its name). The Roster screen also reads find-students; it ignores the new JSON property, so it keeps working untouched (README decision 2).

- [ ] **Step 1: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Queries\GetStudentInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetStudent;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetStudentInteractorTest
{
    private IStudentQueries queries = null!;
    private GetStudentInteractor interactor = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IStudentQueries>();
        interactor = new GetStudentInteractor(queries);
    }

    [TestMethod]
    public async Task Returns_The_Student()
    {
        //given
        var id = Guid.NewGuid();
        var student = new GetStudentResponse
        {
            Id = id,
            NationalId = "123456782",
            Name = "Shaked Navon",
            Phone = "050-3318842",
            TeacherId = Guid.NewGuid(),
            TeacherName = "Yael Carmi",
            CarId = Guid.NewGuid(),
            CarName = "Picanto Red",
            CarTransmission = Transmission.Automatic,
            Address = "12 HaRimon St, Modiin",
            StartDate = new DateOnly(2026, 9, 1),
            LicenseType = "B",
            IsActive = true
        };
        A.CallTo(() => queries.GetAsync(id)).Returns(student);

        //when
        var result = await interactor.ExecuteAsync(id);

        //then
        result.ShouldBe(student);
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var id = Guid.NewGuid();
        A.CallTo(() => queries.GetAsync(id)).Returns((GetStudentResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(id);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }
}
```

In `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`, add after `Week_Schedule_Creation_And_The_Publication_Lifecycle_Stay_Administrator_Only`:

```csharp
    [TestMethod]
    [DataRow("StudentQueryController.FindAsync")]
    [DataRow("StudentQueryController.GetAsync")]
    public void Students_Stay_Administrator_Only(string endpoint)
    {
        //when
        var rule = Endpoints()[endpoint];

        //then
        rule.ShouldBe(AuthorizationPolicies.Administrator);
    }
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetStudentInteractorTest|FullyQualifiedName~ControllerAuthorizationTest"`
Expected: build error `The type or namespace name 'GetStudent' does not exist in the namespace 'DrivingLessons.Application.Queries'`.

- [ ] **Step 3: Add the response, the query method and the not-found overload**

Create `src\DrivingLessons.Application\Queries\GetStudent\GetStudentResponse.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetStudent;

public class GetStudentResponse
{
    public Guid Id { get; init; }
    public string NationalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid CarId { get; init; }
    public string CarName { get; init; } = string.Empty;
    public Transmission CarTransmission { get; init; }
    public string? Address { get; init; }
    public DateOnly? StartDate { get; init; }
    public string? LicenseType { get; init; }
    public bool IsActive { get; init; }
}
```

Replace `src\DrivingLessons.Application\Queries\IStudentQueries.cs` with:

```csharp
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.GetStudent;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IStudentQueries
{
    Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId);

    Task<GetStudentResponse?> GetAsync(Guid id);

    Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart);
}
```

Replace `src\DrivingLessons.Application\Common\Exceptions\StudentNotFoundException.cs` with:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class StudentNotFoundException : NotFoundException
{
    public StudentNotFoundException()
        : base("No active student on the roster matches this national ID.")
    {
    }

    public StudentNotFoundException(StudentId id)
        : base($"Student {id.Value} was not found.")
    {
    }
}
```

- [ ] **Step 4: Add the interactor and register it**

Create `src\DrivingLessons.Application\Queries\GetStudent\GetStudentInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetStudent;

public class GetStudentInteractor
{
    private readonly IStudentQueries queries;

    public GetStudentInteractor(IStudentQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetStudentResponse> ExecuteAsync(Guid id)
    {
        var student = await queries.GetAsync(id);

        if (student is null)
        {
            var studentId = StudentId.Of(id);
            throw new StudentNotFoundException(studentId);
        }

        return student;
    }
}
```

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the using after `using DrivingLessons.Application.Queries.GetPublicationDashboard;`:

```csharp
using DrivingLessons.Application.Queries.GetStudent;
```

and register it right after `services.AddScoped<FindStudentsInteractor>();`:

```csharp
        services.AddScoped<GetStudentInteractor>();
```

- [ ] **Step 5: Run the interactor tests**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetStudentInteractorTest"`
Expected: build error in `DrivingLessons.Infrastructure`: `'StudentQueries' does not implement interface member 'IStudentQueries.GetAsync(Guid)'`. The test project references Infrastructure, so step 6 comes first.

- [ ] **Step 6: Project the transmission and implement `GetAsync`**

Replace `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs` with:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindStudents;

public class ItemForFindStudentsResponse
{
    public Guid Id { get; init; }
    public string NationalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid CarId { get; init; }
    public string CarName { get; init; } = string.Empty;
    public Transmission CarTransmission { get; init; }
    public bool IsActive { get; init; }
}
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`:

1. Add `using DrivingLessons.Application.Queries.GetStudent;` after `using DrivingLessons.Application.Queries.FindStudents;`.
2. In `FindAsync`, in the `select new ItemForFindStudentsResponse` initializer, replace

```csharp
                        CarName = car.Name.Value,
                        IsActive = student.IsActive
```

with

```csharp
                        CarName = car.Name.Value,
                        CarTransmission = car.Transmission,
                        IsActive = student.IsActive
```

3. Add this method between `FindAsync` and `GetActiveByNationalIdAsync`:

```csharp
    public async Task<GetStudentResponse?> GetAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var query = from student in dbContext.Students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    where student.Id == studentId
                    select new GetStudentResponse
                    {
                        Id = student.Id.Value,
                        NationalId = student.NationalId.Value,
                        Name = student.Name.Value,
                        Phone = student.Phone.Value,
                        TeacherId = teacher.Id.Value,
                        TeacherName = teacher.Name.Value,
                        CarId = car.Id.Value,
                        CarName = car.Name.Value,
                        CarTransmission = car.Transmission,
                        Address = student.Address == null
                            ? null
                            : student.Address.Value,
                        StartDate = student.StartDate == null
                            ? null
                            : (DateOnly?)student.StartDate.Value,
                        LicenseType = student.LicenseType == null
                            ? null
                            : student.LicenseType.Value,
                        IsActive = student.IsActive
                    };

        return await query.FirstOrDefaultAsync();
    }
```

(`== null` because this is an expression tree, where `is null` doesn't compile. The `.Value` unwraps run in the final projection, the same way `FindAsync` already unwraps `student.Name.Value`. The joins go through the Teacher and Car query filters, so a Student whose Teacher or Car is deleted is not found, exactly like `FindAsync` leaves them out.)

- [ ] **Step 7: Add the endpoint**

Replace `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentQueryController.cs` with:

```csharp
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.GetStudent;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Student;

[ApiController]
[Route("api/students")]
[Tags("Students")]
public class StudentQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a student")]
    [ProducesResponseType(typeof(GetStudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetStudentResponse> GetAsync(
        [FromServices] GetStudentInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [EndpointSummary("Finds all students, optionally filtered by teacher")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindStudentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(
        [FromServices] FindStudentsInteractor interactor,
        [FromQuery] Guid? teacherId)
    {
        return await interactor.ExecuteAsync(teacherId);
    }
}
```

No `[Authorize]` attribute, like before: the Administrator fallback policy covers it, and `Students_Stay_Administrator_Only` pins it.

- [ ] **Step 8: Run the new tests**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetStudentInteractorTest|FullyQualifiedName~ControllerAuthorizationTest"`
Expected: PASS (2 `GetStudentInteractorTest` tests, every `ControllerAuthorizationTest` test including both `Students_Stay_Administrator_Only` rows).

- [ ] **Step 9: Run every backend suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: build with no new warning, both suites PASS, no pending model changes (nothing in the model changed). The projection itself runs against Postgres in the task 2 smoke (step 12 there).

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application/Queries/FindStudents/ItemForFindStudentsResponse.cs src/DrivingLessons.Application/Queries/GetStudent src/DrivingLessons.Application/Queries/IStudentQueries.cs src/DrivingLessons.Application/Common/Exceptions/StudentNotFoundException.cs src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs src/DrivingLessons.Presentation.Web/Controllers/Student/StudentQueryController.cs tests/DrivingLessons.Application.Test/Queries/GetStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs
git commit -m "feat(students): get a Student and list each Student's Car transmission (#93)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
