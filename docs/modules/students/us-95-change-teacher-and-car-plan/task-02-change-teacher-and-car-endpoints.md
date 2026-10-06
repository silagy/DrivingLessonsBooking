# Task 2 of 7: Change Teacher and Change Car endpoints

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Requires task 1 committed. Work on branch `95-change-teacher-and-car`. Read README decisions 1, 3, 7, 8 and 15 first.

**Files:**
- Create: `src\DrivingLessons.Application\Commands\ChangeStudentTeacher\ChangeStudentTeacherRequest.cs`, `ChangeStudentTeacherInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeStudentCar\ChangeStudentCarRequest.cs`, `ChangeStudentCarInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (after `ReactivateStudentInteractor`, line ~106)
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs`
- Create: `tests\DrivingLessons.Application.Test\Commands\ChangeStudentTeacherInteractorTest.cs`, `ChangeStudentCarInteractorTest.cs`
- Modify: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` (`Students_Stay_Administrator_Only`, line ~89)
- Modify: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (after `Student_Already_Active_Is_A_Conflict_With_Its_Rule_As_Code`)
- Modify: `tests\DrivingLessons.Application.Test\Queries\IdentifyStudentInteractorTest.cs`
- Modify: `client\public\i18n\he.json`, `en.json` (`errors`)

**Interfaces:**
- Consumes: task 1 (`Student.ChangeTeacher(Teacher, Car)`, `Student.ChangeCar(Car)`, `StudentAlreadyWithTeacherException(StudentId, TeacherId)`, `StudentAlreadyOnCarException(StudentId, CarId)`), `IStudentRepository.GetAsync(StudentId)`, `ITeacherRepository.GetAsync(TeacherId)`, `ICarRepository.GetAsync(CarId)`, `StudentNotFoundException(StudentId)`, `TeacherNotFoundException(TeacherId)`, `CarNotFoundException(CarId)` (all in `Application\Common\Exceptions`), `IUnitOfWork.CommitAsync()`.
- Produces (tasks 4 to 7 rely on these):
  - `POST api/students/{id}/change-teacher`, body `{ "teacherId": Guid, "carId": Guid }` → `StudentCommandController.ChangeTeacherAsync`, Administrator-only: 204; 404 `studentNotFound` / `teacherNotFound` / `carNotFound`; 409 `studentAlreadyWithTeacher` / `studentCarMustBeAssignedToTeacher`.
  - `POST api/students/{id}/change-car`, body `{ "carId": Guid }` → `StudentCommandController.ChangeCarAsync`, Administrator-only: 204; 404 `studentNotFound` / `carNotFound`; 409 `studentAlreadyOnCar` / `studentCarMustBeAssignedToTeacher`.
  - Translations `errors.studentAlreadyWithTeacher`, `errors.studentAlreadyOnCar`.

- [ ] **Step 1: Write the failing interactor tests**

`tests\DrivingLessons.Application.Test\Commands\ChangeStudentTeacherInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.ChangeStudentTeacher;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class ChangeStudentTeacherInteractorTest
{
    private IStudentRepository repository = null!;
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeStudentTeacherInteractor interactor = null!;
    private Teacher ronit = null!;
    private Teacher yael = null!;
    private Car corolla = null!;
    private Car i20 = null!;
    private Student noa = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeStudentTeacherInteractor(repository, teacherRepository, carRepository, unitOfWork);

        ronit = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        yael = Teacher.Create(TeacherName.Of("יעל כרמי"), Email.Of("yael@school.example"));
        corolla = Car.Create(CarName.Of("קורולה לבנה"), CarType.Of("קורולה"), Transmission.Automatic);
        i20 = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        corolla.AssignTeacher(ronit);
        i20.AssignTeacher(ronit);
        i20.AssignTeacher(yael);
        noa = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("נועה מזרחי"),
            PhoneNumber.Of("050-1234567"),
            ronit,
            corolla,
            null,
            null,
            null);

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(noa.Id)).Returns(noa);
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(ronit.Id)).Returns(ronit);
        A.CallTo(() => teacherRepository.GetAsync(yael.Id)).Returns(yael);
        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(corolla.Id)).Returns(corolla);
        A.CallTo(() => carRepository.GetAsync(i20.Id)).Returns(i20);
    }

    [TestMethod]
    public async Task Changes_The_Teacher_And_Car()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, i20.Id.Value);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.TeacherId.ShouldBe(yael.Id);
        noa.CarId.ShouldBe(i20.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Current_Teacher_Is_Rejected()
    {
        //given
        var request = new ChangeStudentTeacherRequest(ronit.Id.Value, i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentAlreadyWithTeacherException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Car_Of_Another_Teacher_Is_Rejected()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, corolla.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        noa.TeacherId.ShouldBe(ronit.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Teacher_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentTeacherRequest(Guid.NewGuid(), i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentTeacherRequest(yael.Id.Value, Guid.NewGuid());

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

`tests\DrivingLessons.Application.Test\Commands\ChangeStudentCarInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.ChangeStudentCar;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class ChangeStudentCarInteractorTest
{
    private IStudentRepository repository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeStudentCarInteractor interactor = null!;
    private Car corolla = null!;
    private Car i20 = null!;
    private Car mazda = null!;
    private Student noa = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeStudentCarInteractor(repository, carRepository, unitOfWork);

        var ronit = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        var oren = Teacher.Create(TeacherName.Of("אורן לוי"), Email.Of("oren@school.example"));
        corolla = Car.Create(CarName.Of("קורולה לבנה"), CarType.Of("קורולה"), Transmission.Automatic);
        i20 = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        mazda = Car.Create(CarName.Of("מאזדה 3 אפורה"), CarType.Of("מאזדה 3"), Transmission.Manual);
        corolla.AssignTeacher(ronit);
        i20.AssignTeacher(ronit);
        mazda.AssignTeacher(oren);
        noa = Student.Create(
            NationalId.Of("205374184"),
            StudentName.Of("נועה מזרחי"),
            PhoneNumber.Of("050-1234567"),
            ronit,
            corolla,
            null,
            null,
            null);

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(noa.Id)).Returns(noa);
        A.CallTo(() => carRepository.GetAsync(A<CarId>._)).Returns((Car?)null);
        A.CallTo(() => carRepository.GetAsync(corolla.Id)).Returns(corolla);
        A.CallTo(() => carRepository.GetAsync(i20.Id)).Returns(i20);
        A.CallTo(() => carRepository.GetAsync(mazda.Id)).Returns(mazda);
    }

    [TestMethod]
    public async Task Changes_The_Car()
    {
        //given
        var request = new ChangeStudentCarRequest(i20.Id.Value);

        //when
        await interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        noa.CarId.ShouldBe(i20.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Current_Car_Is_Rejected()
    {
        //given
        var request = new ChangeStudentCarRequest(corolla.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentAlreadyOnCarException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Car_Of_Another_Teacher_Is_Rejected()
    {
        //given
        var request = new ChangeStudentCarRequest(mazda.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<StudentCarMustBeAssignedToTeacherException>(act);
        noa.CarId.ShouldBe(corolla.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentCarRequest(i20.Id.Value);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Car_Is_Not_Found()
    {
        //given
        var request = new ChangeStudentCarRequest(Guid.NewGuid());

        //when
        var act = () => interactor.ExecuteAsync(noa.Id.Value, request);

        //then
        await Should.ThrowAsync<CarNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

If `Teacher.Create`, `Car.Create` or the value objects take different arguments than shown, copy the construction from `DeactivateStudentInteractorTest.ActiveStudent()` and `ChangeStudentDetailsInteractorTest.Init()`, which compile today.

- [ ] **Step 2: Write the failing filter, authorization and identify tests**

In `ApiExceptionFilterTest.cs`, after `Student_Already_Active_Is_A_Conflict_With_Its_Rule_As_Code`:

```csharp
    [TestMethod]
    public void Student_Already_With_Teacher_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentAlreadyWithTeacherException(StudentId.New(), TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentAlreadyWithTeacher");
    }

    [TestMethod]
    public void Student_Already_On_Car_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentAlreadyOnCarException(StudentId.New(), CarId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentAlreadyOnCar");
    }
```

In `ControllerAuthorizationTest.cs`, add two rows above `public void Students_Stay_Administrator_Only(string endpoint)`, after `[DataRow("StudentCommandController.ReactivateAsync")]`:

```csharp
    [DataRow("StudentCommandController.ChangeTeacherAsync")]
    [DataRow("StudentCommandController.ChangeCarAsync")]
```

In `IdentifyStudentInteractorTest.cs`, after `Returns_The_Roster_Student_For_The_Publication_Week`:

```csharp
    [TestMethod]
    public async Task Identifies_With_The_Teacher_The_Student_Has_Now()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);
        var afterChangeTeacher = new IdentifyStudentResponse
        {
            StudentName = "Test Student",
            TeacherName = "Teacher Carmi",
            CarName = "i20 Silver",
            Transmission = Transmission.Manual
        };

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns(afterChangeTeacher);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var response = await interactor.ExecuteAsync(linkToken, request);

        //then
        response.TeacherName.ShouldBe("Teacher Carmi");
        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .MustHaveHappenedOnceExactly();
    }
```

This pins that identification takes the Teacher from the Student's current record for the Publication's week, never from the Publication or an earlier Submission. The query itself (`StudentQueries.GetActiveByNationalIdAsync` joins on `student.TeacherId`) has no unit test project; task 7 step 4 proves it on Postgres.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: build FAIL: `The type or namespace name 'ChangeStudentTeacher' does not exist`, `'ChangeStudentCar' does not exist`.

- [ ] **Step 4: Write the requests and interactors**

`src\DrivingLessons.Application\Commands\ChangeStudentTeacher\ChangeStudentTeacherRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeStudentTeacher;

public record ChangeStudentTeacherRequest(Guid TeacherId, Guid CarId);
```

`src\DrivingLessons.Application\Commands\ChangeStudentTeacher\ChangeStudentTeacherInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeStudentTeacher;

public class ChangeStudentTeacherInteractor
{
    private readonly IStudentRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeStudentTeacherInteractor(
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

    public async Task ExecuteAsync(Guid id, ChangeStudentTeacherRequest request)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var teacherId = TeacherId.Of(request.TeacherId);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var carId = CarId.Of(request.CarId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        student.ChangeTeacher(teacher, car);

        await unitOfWork.CommitAsync();
    }
}
```

`src\DrivingLessons.Application\Commands\ChangeStudentCar\ChangeStudentCarRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeStudentCar;

public record ChangeStudentCarRequest(Guid CarId);
```

`src\DrivingLessons.Application\Commands\ChangeStudentCar\ChangeStudentCarInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeStudentCar;

public class ChangeStudentCarInteractor
{
    private readonly IStudentRepository repository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeStudentCarInteractor(
        IStudentRepository repository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeStudentCarRequest request)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        var carId = CarId.Of(request.CarId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        student.ChangeCar(car);

        await unitOfWork.CommitAsync();
    }
}
```

In `DependencyInjection.cs`, add the two `using`s in alphabetical position (`DrivingLessons.Application.Commands.ChangeStudentCar;` and `...ChangeStudentTeacher;` next to `...ChangeStudentDetails;`) and register them after `services.AddScoped<ReactivateStudentInteractor>();`:

```csharp
        services.AddScoped<ChangeStudentTeacherInteractor>();
        services.AddScoped<ChangeStudentCarInteractor>();
```

- [ ] **Step 5: Add the controller actions**

In `StudentCommandController.cs`, add `using DrivingLessons.Application.Commands.ChangeStudentCar;` and `using DrivingLessons.Application.Commands.ChangeStudentTeacher;` (alphabetical), and after `ReactivateAsync`:

```csharp
    [HttpPost("{id:guid}/change-teacher")]
    [EndpointSummary("Change the student's teacher, together with one of the new teacher's cars")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeTeacherAsync(
        [FromServices] ChangeStudentTeacherInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeStudentTeacherRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPost("{id:guid}/change-car")]
    [EndpointSummary("Change the student's car to another of their teacher's cars")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeCarAsync(
        [FromServices] ChangeStudentCarInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeStudentCarRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }
```

- [ ] **Step 6: Add the error translations**

In `client\public\i18n\he.json`, inside `errors`, next to `studentAlreadyActive` (keep the file's key order style: insert after `studentAlreadyActive`):

```json
    "studentAlreadyWithTeacher": "זה כבר המורה של התלמיד הזה. יש לרענן את המסך.",
    "studentAlreadyOnCar": "זה כבר הרכב של התלמיד הזה. יש לרענן את המסך.",
```

In `client\public\i18n\en.json`, at the same place:

```json
    "studentAlreadyWithTeacher": "This is already this Student's Teacher. Refresh the screen.",
    "studentAlreadyOnCar": "This is already this Student's Car. Refresh the screen.",
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: PASS, every test (the authorization test proves both new actions are Administrator-only).

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/translations.spec.ts`
Expected: PASS (both languages have the same keys, no forbidden characters).

- [ ] **Step 8: Smoke the endpoints against Postgres**

Use the compose Postgres, not `dl-postgres` (memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us95_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us95_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`). It migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us95_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Work in a scratchpad folder (`<scratchpad>` is this session's scratchpad directory). Hebrew on the Windows command line turns into `?????` (memory note), so every Hebrew value is written by `seed.js` and sent by relative `@file` paths:

```bash
mkdir -p "<scratchpad>/smoke-95" && cd "<scratchpad>/smoke-95"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
auth() { echo "Authorization: Bearer $ADMIN"; }
post() { curl -s -o body.json -w "%{http_code}" -X POST "$API/api/$1" -H "$(auth)" -H "Content-Type: application/json" ${2:+-d @"$2"}; }

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

cat > seed.js <<'EOF'
const fs = require('fs');
const write = (file, body) => fs.writeFileSync(file, JSON.stringify(body));
write('ronit.json', { name: 'רונית אברהם', contactEmail: 'ronit@school.example' });
write('yael.json', { name: 'יעל כרמי', contactEmail: 'yael@school.example' });
write('corolla.json', { name: 'קורולה לבנה', type: 'קורולה', transmission: 'automatic' });
write('i20.json', { name: 'i20 כסופה', type: 'i20', transmission: 'manual' });
EOF
node seed.js

echo "ronit $(post teachers ronit.json)"; RONIT=$(json id < body.json)
echo "yael $(post teachers yael.json)"; YAEL=$(json id < body.json)
echo "corolla $(post cars corolla.json)"; COROLLA=$(json id < body.json)
echo "i20 $(post cars i20.json)"; I20=$(json id < body.json)
echo "assign $(post cars/$COROLLA/teachers/$RONIT) $(post cars/$I20/teachers/$RONIT) $(post cars/$I20/teachers/$YAEL)"

node -e "require('fs').writeFileSync('noa.json', JSON.stringify({ nationalId: '205374184', name: 'נועה מזרחי', phone: '050-1234567', teacherId: process.argv[1], carId: process.argv[2], address: null, startDate: null, licenseType: null }))" $RONIT $COROLLA
echo "create noa $(post students noa.json)"; NOA=$(json id < body.json)

body() { node -e "require('fs').writeFileSync(process.argv[1], JSON.stringify(Object.fromEntries(process.argv.slice(2).map(p => p.split('=')))))" "$@"; }
body same-teacher.json teacherId=$RONIT carId=$I20
body car-not-yael.json teacherId=$YAEL carId=$COROLLA
body to-yael.json teacherId=$YAEL carId=$I20
body to-corolla.json carId=$COROLLA
body to-i20.json carId=$I20
body missing-car.json carId=00000000-0000-0000-0000-000000000001

echo "same teacher $(post students/$NOA/change-teacher same-teacher.json) $(json code < body.json)"
echo "car not of new teacher $(post students/$NOA/change-teacher car-not-yael.json) $(json code < body.json)"
echo "change teacher $(post students/$NOA/change-teacher to-yael.json)"
echo "same car $(post students/$NOA/change-car to-i20.json) $(json code < body.json)"
echo "car not of teacher $(post students/$NOA/change-car to-corolla.json) $(json code < body.json)"
echo "missing car $(post students/$NOA/change-car missing-car.json) $(json code < body.json)"
echo "missing student $(post students/00000000-0000-0000-0000-000000000001/change-car to-i20.json) $(json code < body.json)"
curl -s "$API/api/students/$NOA" -H "$(auth)" | json "[teacherId === '$YAEL', carId === '$I20'].join(' ')"
```

Expected:
- `ronit 201`, `yael 201`, `corolla 201`, `i20 201`, `assign` three 2xx codes, `create noa 201`.
- `same teacher 409 studentAlreadyWithTeacher`, `car not of new teacher 409 studentCarMustBeAssignedToTeacher`, `change teacher 204`.
- `same car 409 studentAlreadyOnCar`, `car not of teacher 409 studentCarMustBeAssignedToTeacher`, `missing car 404 carNotFound`, `missing student 404 studentNotFound`.
- The last line: `true true`.

If a route or field differs from what this step assumes (for example the teacher or car create payload), read the controller and adapt the script; don't change the code to fit the script. Stop the API (`TaskStop`) and drop the database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us95_smoke"
```

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Application/Commands/ChangeStudentTeacher src/DrivingLessons.Application/Commands/ChangeStudentCar src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Presentation.Web/Controllers/Student/StudentCommandController.cs tests/DrivingLessons.Application.Test/Commands/ChangeStudentTeacherInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ChangeStudentCarInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs tests/DrivingLessons.Application.Test/Queries/IdentifyStudentInteractorTest.cs client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(students): change a Student's Teacher or Car from the API, refusing the current one (#95)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
