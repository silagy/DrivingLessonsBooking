# Task 2 of 4: Refusal problem details and translations

> Part of [#91: Block deleting or unassigning Teachers and Cars while active Students depend on them](README.md). Requires task 1 committed. Work on branch `91-block-removal-with-active-students`. Read README decisions 6 to 8 first.

**Files:**
- Modify: `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` (`ParamsOf`)
- Modify: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (after `Missing_Roster_Columns_Are_Named_In_Params`)
- Modify: `client\public\i18n\he.json`, `en.json` (`errors`, after `teacherMustNotHaveActiveUser`)

**Interfaces:**
- Consumes: task 1's three exceptions (`ActiveStudentNames: IReadOnlyList<StudentName>`, and `TeacherName: TeacherName` on the assignment one); the filter's existing `CodeOf` (code = type name without `Exception`, camelCase) and `ColumnSeparator = ", "`.
- Produces (tasks 3 and 4 rely on these):
  - 409 problems with `code` `teacherMustNotHaveActiveStudents` / `carMustNotHaveActiveStudents` / `teacherAssignmentMustNotHaveActiveStudents`, `type` null, and `params`: `count` (string, all active Students), `names` (first three, `, `-joined, `...` appended when there are more), plus `teacher` on the assignment one.
  - Translations `errors.teacherMustNotHaveActiveStudents`, `errors.carMustNotHaveActiveStudents`, `errors.teacherAssignmentMustNotHaveActiveStudents` using `{{count}}`, `{{names}}`, `{{teacher}}`.

- [ ] **Step 1: Write the failing filter tests**

In `ApiExceptionFilterTest.cs`, after `Missing_Roster_Columns_Are_Named_In_Params`:

```csharp
    [TestMethod]
    public void Teacher_With_Active_Students_Is_A_Conflict_Naming_Them()
    {
        //given
        var names = new[] { StudentName.Of("Avi Cohen"), StudentName.Of("Noa Mizrahi") };
        var context = ContextFor(new TeacherMustNotHaveActiveStudentsException(TeacherId.New(), names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Type.ShouldBeNull();
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherMustNotHaveActiveStudents");
        var parameters = problem.Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["count"] = "2", ["names"] = "Avi Cohen, Noa Mizrahi" });
    }

    [TestMethod]
    public void Car_With_Active_Students_Is_A_Conflict_Naming_Them()
    {
        //given
        var names = new[] { StudentName.Of("נועה מזרחי") };
        var context = ContextFor(new CarMustNotHaveActiveStudentsException(CarId.New(), names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "carMustNotHaveActiveStudents");
        var parameters = problem.Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["count"] = "1", ["names"] = "נועה מזרחי" });
    }

    [TestMethod]
    public void Teacher_Assignment_With_Active_Students_Is_A_Conflict_Naming_The_Teacher_And_Them()
    {
        //given
        var names = new[] { StudentName.Of("Noa Mizrahi") };
        var context = ContextFor(new TeacherAssignmentMustNotHaveActiveStudentsException(
            CarId.New(),
            TeacherId.New(),
            TeacherName.Of("Ronit Avraham"),
            names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherAssignmentMustNotHaveActiveStudents");
        var parameters = problem.Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string>
        {
            ["count"] = "1",
            ["names"] = "Noa Mizrahi",
            ["teacher"] = "Ronit Avraham"
        });
    }

    [TestMethod]
    public void Active_Students_Beyond_Three_Are_Counted_Not_Named()
    {
        //given
        var names = new[]
        {
            StudentName.Of("Avi Cohen"),
            StudentName.Of("Dana Sasson"),
            StudentName.Of("Lia Hadad"),
            StudentName.Of("Noa Mizrahi"),
            StudentName.Of("Omer Shalev")
        };
        var context = ContextFor(new CarMustNotHaveActiveStudentsException(CarId.New(), names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var parameters = ProblemOf(context).Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["count"] = "5", ["names"] = "Avi Cohen, Dana Sasson, Lia Hadad..." });
    }
```

If `StudentName.Of` or `TeacherName.Of` rejects these values, use any valid name; keep the expected strings in step with them.

- [ ] **Step 2: Run the filter tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest"`
Expected: the four new tests FAIL with `KeyNotFoundException` on `"params"` (the code and status assertions already pass: the filter maps every `DomainException` to 409 with its code).

- [ ] **Step 3: Add the params**

In `ApiExceptionFilter.cs`, add `using DrivingLessons.Domain.Values;` (alphabetical, after `DrivingLessons.Domain.Exceptions`), and two constants after `ColumnSeparator`:

```csharp
    private const int NamedStudentsLimit = 3;
    private const string MoreNamesSuffix = "...";
```

Add three arms to the `ParamsOf` switch, before `_ => null`:

```csharp
            TeacherMustNotHaveActiveStudentsException teacher => ActiveStudentsParams(teacher.ActiveStudentNames),
            CarMustNotHaveActiveStudentsException car => ActiveStudentsParams(car.ActiveStudentNames),
            TeacherAssignmentMustNotHaveActiveStudentsException assignment => AssignmentParams(assignment),
```

and the helpers after `ParamsOf`:

```csharp
    private static Dictionary<string, string> AssignmentParams(TeacherAssignmentMustNotHaveActiveStudentsException assignment)
    {
        var parameters = ActiveStudentsParams(assignment.ActiveStudentNames);
        parameters["teacher"] = assignment.TeacherName.Value;

        return parameters;
    }

    private static Dictionary<string, string> ActiveStudentsParams(IReadOnlyList<StudentName> activeStudentNames)
    {
        var namedStudents = activeStudentNames
                            .Take(NamedStudentsLimit)
                            .Select(x => x.Value);
        var joinedNames = string.Join(ColumnSeparator, namedStudents);
        var names = activeStudentNames.Count > NamedStudentsLimit
            ? joinedNames + MoreNamesSuffix
            : joinedNames;

        return new Dictionary<string, string>
        {
            ["count"] = activeStudentNames.Count.ToString(CultureInfo.InvariantCulture),
            ["names"] = names
        };
    }
```

Every arm of the switch is a `Dictionary<string, string>` (or `null`), so it keeps converting to the method's `IReadOnlyDictionary<string, string>?` return type.

- [ ] **Step 4: Run the filter tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: PASS, every test.

- [ ] **Step 5: Check the source-text rule**

The `...` suffix is three plain dots, not the ellipsis character. Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~SourceText"`
Expected: PASS (or "No test matches" if the source-text test lives in another project; then run that project's test the same way, `git grep -l SourceTextTest tests` finds it).

- [ ] **Step 6: Add the error translations**

In `client\public\i18n\he.json`, inside `errors`, right after `teacherMustNotHaveActiveUser`:

```json
    "teacherMustNotHaveActiveStudents": "למורה הזה יש תלמידים פעילים ({{count}}): {{names}}. יש להחליף להם מורה במסך התלמידים, ואז למחוק.",
    "carMustNotHaveActiveStudents": "תלמידים פעילים לומדים על הרכב הזה ({{count}}): {{names}}. יש להחליף להם רכב במסך התלמידים, ואז למחוק.",
    "teacherAssignmentMustNotHaveActiveStudents": "תלמידים פעילים של {{teacher}} לומדים על הרכב הזה ({{count}}): {{names}}. יש להחליף להם רכב במסך התלמידים, ואז להסיר את השיוך.",
```

In `client\public\i18n\en.json`, at the same place:

```json
    "teacherMustNotHaveActiveStudents": "This Teacher still has active Students ({{count}}): {{names}}. Change their Teacher on the Students screen, then delete.",
    "carMustNotHaveActiveStudents": "Active Students learn on this Car ({{count}}): {{names}}. Change their Car on the Students screen, then delete.",
    "teacherAssignmentMustNotHaveActiveStudents": "Active Students of {{teacher}} learn on this Car ({{count}}): {{names}}. Change their Car on the Students screen, then unassign.",
```

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/translations.spec.ts`
Expected: PASS (both languages have the same keys, no forbidden characters).

- [ ] **Step 7: Start a throwaway database and the API**

Use the compose Postgres, not `dl-postgres` (memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us91_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us91_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`). It migrates and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us91_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

- [ ] **Step 8: Smoke every guard against Postgres**

This proves the part no unit test reaches: `FindByTeacherAsync` / `FindByCarAsync` return inactive Students too and the domain lets them through, and a shared Car's other Teacher doesn't block (Review Focus 1).

Work in a scratchpad folder (`<scratchpad>` is this session's scratchpad directory). Hebrew on the Windows command line turns into `?????` (memory note), so every Hebrew value is written by `seed.js`, sent by relative `@file` paths, and compared inside node:

```bash
mkdir -p "<scratchpad>/smoke-91" && cd "<scratchpad>/smoke-91"
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
auth() { echo "Authorization: Bearer $ADMIN"; }
post() { curl -s -o body.json -w "%{http_code}" -X POST "$API/api/$1" -H "$(auth)" -H "Content-Type: application/json" ${2:+-d @"$2"}; }
del() { curl -s -o body.json -w "%{http_code}" -X DELETE "$API/api/$1" -H "$(auth)"; }
refusal() { node -e "const b = JSON.parse(require('fs').readFileSync('body.json', 'utf8')); const n = f => JSON.parse(require('fs').readFileSync(f, 'utf8')).name; console.log(b.code, b.params.count, b.params.names === n('$1'), '$2' ? b.params.teacher === n('$2') : '')"; }

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

student() { node -e "require('fs').writeFileSync(process.argv[1], JSON.stringify({ nationalId: process.argv[2], name: process.argv[3] === 'noa' ? 'נועה מזרחי' : 'עומר שלו', phone: '050-1234567', teacherId: process.argv[4], carId: process.argv[5], address: null, startDate: null, licenseType: null }))" "$@"; }
student noa.json 205374184 noa $RONIT $COROLLA
student omer.json 312456783 omer $YAEL $I20
echo "create noa $(post students noa.json)"; NOA=$(json id < body.json)
echo "create omer $(post students omer.json)"; OMER=$(json id < body.json)

echo "unassign corolla/ronit $(del cars/$COROLLA/teachers/$RONIT) $(refusal noa.json ronit.json)"
echo "delete corolla $(del cars/$COROLLA) $(refusal noa.json)"
echo "delete ronit $(del teachers/$RONIT) $(refusal noa.json)"
echo "delete i20 $(del cars/$I20) $(refusal omer.json)"

echo "deactivate noa $(post students/$NOA/deactivate)"
echo "unassign i20/ronit $(del cars/$I20/teachers/$RONIT)"
echo "unassign corolla/ronit $(del cars/$COROLLA/teachers/$RONIT)"
echo "delete corolla $(del cars/$COROLLA)"
echo "delete ronit $(del teachers/$RONIT)"
echo "delete i20 still $(del cars/$I20) $(refusal omer.json)"
```

Expected:
- `ronit 201`, `yael 201`, `corolla 201`, `i20 201`, `assign` three 2xx codes, `create noa 201`, `create omer 201`.
- `unassign corolla/ronit 409 teacherAssignmentMustNotHaveActiveStudents 1 true true`.
- `delete corolla 409 carMustNotHaveActiveStudents 1 true`, `delete ronit 409 teacherMustNotHaveActiveStudents 1 true`, `delete i20 409 carMustNotHaveActiveStudents 1 true`.
- `deactivate noa 204`.
- `unassign i20/ronit 204` (Omer is Yael's Student on the shared i20 and doesn't block Ronit).
- `unassign corolla/ronit 204`, `delete corolla 204`, `delete ronit 204` (Noa is inactive).
- `delete i20 still 409 carMustNotHaveActiveStudents 1 true`.

If a route or field differs from what this step assumes (for example the deactivate route or the Teacher create payload), read the controller and adapt the script; don't change the code to fit the script. Stop the API (`TaskStop`) and drop the database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us91_smoke"
```

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(teachers): name the active Students in the 409 refusals and translate them (#91)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
