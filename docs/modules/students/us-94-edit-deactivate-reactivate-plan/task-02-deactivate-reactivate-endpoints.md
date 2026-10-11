# Task 2 of 7: Deactivate and Reactivate endpoints (backend)

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Requires task 1 committed. Work on branch `94-edit-deactivate-reactivate-students`. Read README decisions 5, 7, 13 and 20 first.

**Files:**
- Create: `src\DrivingLessons.Application\Commands\DeactivateStudent\DeactivateStudentInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\ReactivateStudent\ReactivateStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (two `errors.*` texts)
- Test: `tests\DrivingLessons.Application.Test\Commands\DeactivateStudentInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Commands\ReactivateStudentInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`

**Interfaces:**
- Consumes: `IStudentRepository.GetAsync(StudentId)` (task 1), `Student.Deactivate()` (throws `StudentAlreadyDeactivatedException(StudentId)`), `Student.Reactivate()` (throws `StudentAlreadyActiveException(StudentId)`), `StudentNotFoundException(StudentId)`, `IUnitOfWork.CommitAsync()`, `PUT api/students/{id}/details` (task 1, smoked here).
- Produces (tasks 4 to 7 rely on these):
  - `POST api/students/{id}/deactivate` → `StudentCommandController.DeactivateAsync`, Administrator-only: 204; 404 `studentNotFound`; 409 `studentAlreadyDeactivated`.
  - `POST api/students/{id}/reactivate` → `StudentCommandController.ReactivateAsync`, Administrator-only: 204; 404 `studentNotFound`; 409 `studentAlreadyActive`.
  - `errors.studentAlreadyDeactivated` = "התלמיד הזה כבר לא פעיל. הרשימה רועננה." / "This Student is already inactive. The list has been refreshed."; `errors.studentAlreadyActive` = "התלמיד הזה כבר פעיל. הרשימה רועננה." / "This Student is already active. The list has been refreshed.".

**Why:** AC "Deactivate / Reactivate command endpoints use the existing domain methods; deactivating an Inactive Student and reactivating an active one are rejected with translated 409s" and "`ApiExceptionFilterTest` extended for each new exception". The interactors add no rule: the aggregate's existing guards are the rule.

- [ ] **Step 1: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Commands\DeactivateStudentInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.DeactivateStudent;
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
public class DeactivateStudentInteractorTest
{
    private IStudentRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeactivateStudentInteractor interactor = null!;
    private Student itai = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeactivateStudentInteractor(repository, unitOfWork);
        itai = ActiveStudent();

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(itai.Id)).Returns(itai);
    }

    [TestMethod]
    public async Task Deactivates_An_Active_Student()
    {
        //when
        await interactor.ExecuteAsync(itai.Id.Value);

        //then
        itai.IsActive.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Inactive_Student_Is_Rejected()
    {
        //given
        itai.Deactivate();

        //when
        var act = () => interactor.ExecuteAsync(itai.Id.Value);

        //then
        await Should.ThrowAsync<StudentAlreadyDeactivatedException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student ActiveStudent()
    {
        var teacher = Teacher.Create(TeacherName.Of("יעל כרמי"), Email.Of("yael@school.example"));
        var car = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        car.AssignTeacher(teacher);

        return Student.Create(
            NationalId.Of("207815432"),
            StudentName.Of("איתי פרץ"),
            PhoneNumber.Of("050-6612034"),
            teacher,
            car,
            null,
            null,
            null);
    }
}
```

Create `tests\DrivingLessons.Application.Test\Commands\ReactivateStudentInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.ReactivateStudent;
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
public class ReactivateStudentInteractorTest
{
    private IStudentRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ReactivateStudentInteractor interactor = null!;
    private Student dana = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IStudentRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ReactivateStudentInteractor(repository, unitOfWork);
        dana = InactiveStudent();

        A.CallTo(() => repository.GetAsync(A<StudentId>._)).Returns((Student?)null);
        A.CallTo(() => repository.GetAsync(dana.Id)).Returns(dana);
    }

    [TestMethod]
    public async Task Reactivates_An_Inactive_Student()
    {
        //when
        await interactor.ExecuteAsync(dana.Id.Value);

        //then
        dana.IsActive.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Active_Student_Is_Rejected()
    {
        //given
        dana.Reactivate();

        //when
        var act = () => interactor.ExecuteAsync(dana.Id.Value);

        //then
        await Should.ThrowAsync<StudentAlreadyActiveException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Student_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static Student InactiveStudent()
    {
        var teacher = Teacher.Create(TeacherName.Of("רונית אברהם"), Email.Of("ronit@school.example"));
        var car = Car.Create(CarName.Of("i20 כסופה"), CarType.Of("i20"), Transmission.Manual);
        car.AssignTeacher(teacher);

        var student = Student.Create(
            NationalId.Of("311078547"),
            StudentName.Of("דנה ששון"),
            PhoneNumber.Of("052-9038816"),
            teacher,
            car,
            null,
            null,
            null);
        student.Deactivate();

        return student;
    }
}
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DeactivateStudentInteractorTest|FullyQualifiedName~ReactivateStudentInteractorTest"`
Expected: build error `The type or namespace name 'DeactivateStudent' does not exist in the namespace 'DrivingLessons.Application.Commands'` (and the same for `ReactivateStudent`).

- [ ] **Step 3: Add the two interactors**

Create `src\DrivingLessons.Application\Commands\DeactivateStudent\DeactivateStudentInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeactivateStudent;

public class DeactivateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeactivateStudentInteractor(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        student.Deactivate();

        await unitOfWork.CommitAsync();
    }
}
```

Create `src\DrivingLessons.Application\Commands\ReactivateStudent\ReactivateStudentInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReactivateStudent;

public class ReactivateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ReactivateStudentInteractor(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        student.Reactivate();

        await unitOfWork.CommitAsync();
    }
}
```

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the two usings in alphabetical position among the `Commands` usings (`DeactivateStudent` after `CreateUser`; `ReactivateStudent` before `RestoreUser`):

```csharp
using DrivingLessons.Application.Commands.DeactivateStudent;
using DrivingLessons.Application.Commands.ReactivateStudent;
```

and register them right after `services.AddScoped<ChangeStudentDetailsInteractor>();` (task 1):

```csharp
        services.AddScoped<DeactivateStudentInteractor>();
        services.AddScoped<ReactivateStudentInteractor>();
```

- [ ] **Step 4: Run the interactor tests**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DeactivateStudentInteractorTest|FullyQualifiedName~ReactivateStudentInteractorTest"`
Expected: PASS, 6 tests.

- [ ] **Step 5: Write the filter and policy tests**

Add to `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, right after `Student_Car_Not_Of_Teacher_Is_A_Conflict_With_Its_Rule_As_Code`:

```csharp
    [TestMethod]
    public void Student_Already_Deactivated_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentAlreadyDeactivatedException(StudentId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentAlreadyDeactivated");
    }

    [TestMethod]
    public void Student_Already_Active_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentAlreadyActiveException(StudentId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentAlreadyActive");
    }

    [TestMethod]
    public void Student_Not_Found_By_Id_Is_A_Not_Found_With_Its_Code()
    {
        //given
        var context = ContextFor(new StudentNotFoundException(StudentId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status404NotFound);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentNotFound");
    }
```

(If `StudentId.New()` doesn't exist, use `StudentId.Of(Guid.NewGuid())`; `CarId.New()` / `TeacherId.New()` are used the same way in this file.)

In `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`, add two rows to `Students_Stay_Administrator_Only`:

```csharp
    [TestMethod]
    [DataRow("StudentQueryController.FindAsync")]
    [DataRow("StudentQueryController.GetAsync")]
    [DataRow("StudentCommandController.CreateAsync")]
    [DataRow("StudentCommandController.ChangeDetailsAsync")]
    [DataRow("StudentCommandController.DeactivateAsync")]
    [DataRow("StudentCommandController.ReactivateAsync")]
    public void Students_Stay_Administrator_Only(string endpoint)
```

- [ ] **Step 6: Run them to see the policy rows fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest|FullyQualifiedName~ControllerAuthorizationTest"`
Expected: FAIL only in the `DeactivateAsync` and `ReactivateAsync` rows (`KeyNotFoundException`). The three filter tests already pass: every `DomainException` is a 409 and every `NotFoundException` a 404 with the type name as `code`; they pin the codes the client keys on (README decision 5).

- [ ] **Step 7: Add the two endpoints**

In `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs`, add the usings (alphabetical, after `using DrivingLessons.Application.Commands.CreateStudent;`):

```csharp
using DrivingLessons.Application.Commands.DeactivateStudent;
using DrivingLessons.Application.Commands.ReactivateStudent;
```

and add these two actions after `ChangeDetailsAsync`:

```csharp
    [HttpPost("{id:guid}/deactivate")]
    [EndpointSummary("Deactivate the student so they can no longer submit on the student form")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(
        [FromServices] DeactivateStudentInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [EndpointSummary("Reactivate an inactive student so they can submit again")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateAsync(
        [FromServices] ReactivateStudentInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }
```

- [ ] **Step 8: Reword the two refusals and run every suite**

The Students screen is the only place these codes surface, and it reloads the list on them (README decision 13), so the copy deck's `err.alreadyInactive` / `err.alreadyActive` apply. In `client\public\i18n\he.json`, replace

```json
    "studentAlreadyActive": "התלמיד הזה כבר פעיל.",
    "studentAlreadyDeactivated": "התלמיד הזה כבר לא פעיל.",
```

with

```json
    "studentAlreadyActive": "התלמיד הזה כבר פעיל. הרשימה רועננה.",
    "studentAlreadyDeactivated": "התלמיד הזה כבר לא פעיל. הרשימה רועננה.",
```

In `client\public\i18n\en.json`, replace

```json
    "studentAlreadyActive": "This student is already active.",
    "studentAlreadyDeactivated": "This student is already inactive.",
```

with

```json
    "studentAlreadyActive": "This Student is already active. The list has been refreshed.",
    "studentAlreadyDeactivated": "This Student is already inactive. The list has been refreshed.",
```

(If the two keys aren't adjacent in a file, replace each line where it is.)

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

Expected: everything PASS (including `Every_Other_Endpoint_Is_Administrator_Only`); no pending model changes.

- [ ] **Step 9: Smoke tasks 1 and 2 against Postgres (Review Focus 1 and 2)**

Use the compose Postgres, not `dl-postgres` (memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us94_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us94_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`). It migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us94_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Work in a scratchpad folder (`<scratchpad>` is this session's scratchpad directory). Hebrew on the Windows command line turns into `?????` (memory note), so every Hebrew value is written by `seed.js` and sent by relative `@file` paths:

```bash
mkdir -p "<scratchpad>/smoke-94" && cd "<scratchpad>/smoke-94"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
auth() { echo "Authorization: Bearer $ADMIN"; }

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

cat > seed.js <<'EOF'
const fs = require('fs');
const write = (file, body) => fs.writeFileSync(file, JSON.stringify(body));
write('teacher.json', { name: 'רונית אברהם', contactEmail: 'ronit@school.example' });
write('car.json', { name: 'קורולה לבנה', type: 'קורולה', transmission: 'automatic' });
fs.writeFileSync('noa-name.txt', 'נועה מזרחי');
fs.writeFileSync('omer-name.txt', 'עומר שלו');
EOF
node seed.js

TEACHER=$(curl -s -X POST $API/api/teachers -H "$(auth)" -H "Content-Type: application/json" -d @teacher.json | json id)
CAR=$(curl -s -X POST $API/api/cars -H "$(auth)" -H "Content-Type: application/json" -d @car.json | json id)
curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/$CAR/teachers/$TEACHER" -H "$(auth)"

payload() {
  node -e "
const fs = require('fs');
const [file, nameFile, nationalId, address, startDate, licenseType, teacherId, carId] = process.argv.slice(1);
const nullable = (value) => (value === '-' ? null : value);
const body = { nationalId, name: fs.readFileSync(nameFile, 'utf8'), phone: '050-1234567', address: nullable(address), startDate: nullable(startDate), licenseType: nullable(licenseType) };
if (teacherId) { body.teacherId = teacherId; body.carId = carId; }
fs.writeFileSync(file, JSON.stringify(body));
" "$@"
}
call() { curl -s -o body.json -w "%{http_code}" -X "$1" "$API/api/students$2" -H "$(auth)" -H "Content-Type: application/json" ${3:+-d @"$3"}; }

payload noa.json noa-name.txt 205374184 "הרימון 12" 2026-09-01 B $TEACHER $CAR
payload omer.json omer-name.txt 312456783 - - - $TEACHER $CAR
echo "create noa $(call POST "" noa.json)"; NOA=$(json id < body.json)
echo "create omer $(call POST "" omer.json)"; OMER=$(json id < body.json)

payload same.json noa-name.txt 205374184 "הרימון 12" 2026-09-01 B
echo "own id $(call PUT "/$NOA/details" same.json)"
payload taken.json noa-name.txt 312456783 - - -
echo "taken $(call PUT "/$NOA/details" taken.json) $(json code < body.json) $(node -pe "JSON.parse(require('fs').readFileSync('body.json','utf8')).params.name === require('fs').readFileSync('omer-name.txt','utf8')")"
payload cleared.json noa-name.txt 205374184 - - -
echo "cleared $(call PUT "/$NOA/details" cleared.json)"
curl -s "$API/api/students/$NOA" -H "$(auth)" | json "[address, startDate, licenseType].join(',')"
payload invalid.json noa-name.txt 205374185 - - -
echo "invalid $(call PUT "/$NOA/details" invalid.json) $(json code < body.json)"
echo "edit missing $(call PUT "/00000000-0000-0000-0000-000000000001/details" same.json) $(json code < body.json)"

echo "deactivate $(call POST "/$NOA/deactivate")"
echo "deactivate again $(call POST "/$NOA/deactivate") $(json code < body.json)"
payload inactive-edit.json noa-name.txt 205374184 - - B
echo "edit inactive $(call PUT "/$NOA/details" inactive-edit.json)"
echo "reactivate $(call POST "/$NOA/reactivate")"
echo "reactivate again $(call POST "/$NOA/reactivate") $(json code < body.json)"
echo "missing $(call POST "/00000000-0000-0000-0000-000000000001/deactivate") $(json code < body.json)"
curl -s "$API/api/students/find" -H "$(auth)" | json "map(x => x.isActive).join(' ')"
```

Expected:
- `assign 204` (or `201`), `create noa 201`, `create omer 201`.
- `own id 204` (Review Focus 1), `taken 409 studentNationalIdAlreadyInUse true`, `cleared 204`, then `,,` (address, start date and license type are all `null`, Review Focus 2).
- `invalid 409 nationalIdMustHaveValidCheckDigit`, `edit missing 404 studentNotFound`.
- `deactivate 204`, `deactivate again 409 studentAlreadyDeactivated`, `edit inactive 204`, `reactivate 204`, `reactivate again 409 studentAlreadyActive`, `missing 404 studentNotFound`.
- The last line: `true true`.

If a route or field differs from what this step assumes, read the controller and adapt the script; don't change the code to fit the script. Stop the API (`TaskStop`) and drop the database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us94_smoke"
```

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application/Commands/DeactivateStudent src/DrivingLessons.Application/Commands/ReactivateStudent src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Presentation.Web/Controllers/Student/StudentCommandController.cs client/public/i18n/he.json client/public/i18n/en.json tests/DrivingLessons.Application.Test/Commands/DeactivateStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/ReactivateStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs
git commit -m "feat(students): deactivate and reactivate a Student, refusing a toggle that is already done (#94)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
