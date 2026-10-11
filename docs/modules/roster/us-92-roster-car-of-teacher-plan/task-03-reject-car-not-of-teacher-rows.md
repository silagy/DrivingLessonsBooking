# Task 3 of 4: A Roster row whose Car is not its Teacher's is rejected and listed (backend + client)

> Part of [#92: Roster import stops deactivating Students and enforces that a Student's Car is one of their Teacher's Cars](README.md). Requires tasks 1 and 2 committed. Work on branch `92-roster-car-of-teacher`. Read README decisions 5 and 7 and Review Focus 1, 2, 3 and 5 first.

**Files:**
- Modify: `src\DrivingLessons.Domain\Values\RosterRowFailureReason.cs`
- Modify: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`
- Modify (client): `client\src\app\features\roster\domain\roster-row-failure-reason.enum.ts`
- Modify: `client\public\i18n\he.json`, `en.json`
- Test (client): `client\src\app\features\roster\ui\components\failed-rows-panel\failed-rows-panel.component.spec.ts` (**new**)

**Interfaces:**
- Consumes:
  - Task 1: `Car.IsAssignedTo(Teacher teacher)`; the `Student` guard (`StudentCarMustBeAssignedToTeacherException`) as the backstop the import must never trigger.
  - Task 2: `ImportRosterResponse(Guid RosterImportId, int Added, int Updated, int Failed)`; `ImportRosterInteractorTest` fields `teacher`, `car` (assigned to `teacher` in `Init`), `persistedImport`, helpers `Row`, `RowsAre`, `StudentsAre`, `ExistingStudent`, `Request`.
  - Client: `FailedRowsPanelComponent` (`input.required<FailureForGetLatestRosterImportResponse[]>()` named `failures`), which renders `'roster.failureReasons.' + failure.reason | transloco` in `.failed-panel__reason`.
- Produces:
  - `RosterRowFailureReason.CarNotAssignedToTeacher = 100`, serialized `"carNotAssignedToTeacher"` (the API's `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`).
  - Client `RosterRowFailureReason.carNotAssignedToTeacher = 'carNotAssignedToTeacher'`; keys `roster.failureReasons.carNotAssignedToTeacher` = he "הרכב לא משויך למורה הזה", en "Car is not assigned to this Teacher".

**Why:** AC 3, 5, 6, 7; spec story 58. The check sits in the interactor, before `Student.Create` / `UpdateFromRoster`, so a mismatched row becomes report data instead of a 409 that would abort the whole file (the issue's reason for landing the invariant before manual Student management). It runs after the Car resolves and before the start date is parsed: "unknown Car" stays the more specific reason when the Car doesn't exist at all.

- [ ] **Step 1: Write the failing interactor tests**

In `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`:

1. Add fields next to `teacher` and `car`:

```csharp
    private Teacher otherTeacher = null!;
    private Car otherCar = null!;
```

2. In `Init`, after `car.AssignTeacher(teacher);` (task 1), add a second Teacher with their own Car, and return both from the repositories (replace the two existing `FindActiveAsync` setups):

```csharp
        otherTeacher = Teacher.Create(TeacherName.Of("רינה גל"), Email.Of("rina@school.co.il"));
        otherCar = Car.Create(CarName.Of("מאזדה 2"), CarType.Of("מאזדה"), Transmission.Automatic);
        otherCar.AssignTeacher(otherTeacher);

        A.CallTo(() => teacherRepository.FindActiveAsync())
            .Returns(new List<Teacher> { teacher, otherTeacher });

        A.CallTo(() => carRepository.FindActiveAsync())
            .Returns(new List<Car> { car, otherCar });
```

3. Add a helper next to `Row`:

```csharp
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
```

4. Add the tests after `Unknown_Car_Is_Recorded_As_Failed_Row`:

```csharp
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
```

5. Add the helper that builds a Student who already violates the invariant, the way old data does: created consistently, then the Car is unassigned from the Teacher (nothing checked that before #92):

```csharp
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
```

`using DrivingLessons.Domain.Events;` is already there from task 2.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ImportRosterInteractorTest"`
Expected: build error `'RosterRowFailureReason' does not contain a definition for 'CarNotAssignedToTeacher'`.

- [ ] **Step 3: Add the failure reason**

`src\DrivingLessons.Domain\Values\RosterRowFailureReason.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum RosterRowFailureReason
{
    InvalidNationalId = 10,
    DuplicateNationalId = 20,
    MissingName = 30,
    MissingPhone = 40,
    MissingTeacher = 50,
    MissingCar = 60,
    UnknownTeacher = 70,
    UnknownCar = 80,
    InvalidStartDate = 90,
    CarNotAssignedToTeacher = 100
}
```

- [ ] **Step 4: Run again to see the behavior fail**

Run the same filter.
Expected: FAIL. `Car_Not_Of_Teacher_Is_Recorded_As_Failed_Row`, `Car_Not_Of_Teacher_Row_Does_Not_Stop_Other_Rows`, `Car_Not_Of_Teacher_Row_Leaves_Existing_Student_Unchanged` and `Mismatched_Row_Still_Claims_Its_National_Id` throw `StudentCarMustBeAssignedToTeacherException` (the task 1 backstop firing: exactly the whole-file abort this task removes). `Shared_Car_Row_Is_Imported` and both `Existing_Violator_...` tests already pass.

- [ ] **Step 5: Check the invariant per row in the import**

In `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`, `ProcessRow`, right after the Car lookup:

```csharp
        var carName = CarName.Of(row.CarName);

        if (!carsByName.TryGetValue(carName, out var car))
        {
            return RosterRowFailureReason.UnknownCar;
        }

        if (!car.IsAssignedTo(teacher))
        {
            return RosterRowFailureReason.CarNotAssignedToTeacher;
        }

        LessonsStartDate? startDate = null;
```

Nothing else changes: `seenIds.Add(nationalId)` already ran earlier in `ProcessRow`, which is what makes `Mismatched_Row_Still_Claims_Its_National_Id` hold.

- [ ] **Step 6: Run the backend suites**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: PASS, and no pending model changes (the failure `reason` column is an integer; a new enum value needs no migration).

- [ ] **Step 7: Write the failing client spec**

Create `client\src\app\features\roster\ui\components\failed-rows-panel\failed-rows-panel.component.spec.ts`. It loads the real `he.json` and `en.json` so it proves the key exists and reads correctly in both languages:

```ts
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import en from '../../../../../../../public/i18n/en.json';
import he from '../../../../../../../public/i18n/he.json';
import { FailureForGetLatestRosterImportResponse } from '../../../data/get-latest-roster-import.response';
import { RosterRowFailureReason } from '../../../domain/roster-row-failure-reason.enum';
import { FailedRowsPanelComponent } from './failed-rows-panel.component';

const CAR_NOT_OF_TEACHER: FailureForGetLatestRosterImportResponse = {
    rowNumber: 3,
    studentName: 'דנה כהן',
    reason: RosterRowFailureReason.carNotAssignedToTeacher,
};

async function reasonShownIn(lang: 'he' | 'en'): Promise<string | undefined> {
    TestBed.configureTestingModule({
        imports: [
            FailedRowsPanelComponent,
            TranslocoTestingModule.forRoot({
                langs: { he, en },
                translocoConfig: { availableLangs: ['he', 'en'], defaultLang: lang },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(FailedRowsPanelComponent);
    fixture.componentRef.setInput('failures', [CAR_NOT_OF_TEACHER]);
    await fixture.whenStable();

    return (fixture.nativeElement as HTMLElement).querySelector('.failed-panel__reason')?.textContent?.trim();
}

describe('FailedRowsPanelComponent', () => {
    it('explains in Hebrew that the Car is not assigned to the Teacher', async () => {
        //when
        const reason = await reasonShownIn('he');

        //then
        expect(reason).toBe('הרכב לא משויך למורה הזה');
    });

    it('explains in English that the Car is not assigned to the Teacher', async () => {
        //when
        const reason = await reasonShownIn('en');

        //then
        expect(reason).toBe('Car is not assigned to this Teacher');
    });
});
```

Check the relative depth of the JSON import against `client\src\app\core\translations.spec.ts` (`../../../public/i18n/en.json` from `src\app\core\`): from `src\app\features\roster\ui\components\failed-rows-panel\` it is seven `../`. If `TranslocoTestingModule` needs `preloadLangs: true` for the non-default language to resolve synchronously, add it to `forRoot`.

- [ ] **Step 8: Run it to see it fail**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/roster/ui/components/failed-rows-panel/failed-rows-panel.component.spec.ts
```

Expected: FAIL with a TypeScript error: `Property 'carNotAssignedToTeacher' does not exist on type 'typeof RosterRowFailureReason'`.

- [ ] **Step 9: Add the reason to the client and translate it**

`client\src\app\features\roster\domain\roster-row-failure-reason.enum.ts`, add after `invalidStartDate`:

```ts
    carNotAssignedToTeacher = 'carNotAssignedToTeacher',
```

`client\public\i18n\he.json`, inside `roster.failureReasons`, after `"invalidStartDate"`:

```json
  "carNotAssignedToTeacher": "הרכב לא משויך למורה הזה"
```

`client\public\i18n\en.json`, same place:

```json
  "carNotAssignedToTeacher": "Car is not assigned to this Teacher"
```

(Mind the comma after the previous `invalidStartDate` entry in both files; keep each file's existing indentation.)

The panel's markup needs no change: the reason sits in its own `<span>` after the "Row {n} · {name}" label, the name is already direction-isolated, and the panel uses logical properties, so the Hebrew text is RTL-correct.

- [ ] **Step 10: Run every client check**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec passes (both new panel tests, `translations.spec.ts`, `source-text.spec.ts`); the build succeeds.

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Domain/Values/RosterRowFailureReason.cs src/DrivingLessons.Application/Commands/ImportRoster/ImportRosterInteractor.cs tests/DrivingLessons.Application.Test/Commands/ImportRosterInteractorTest.cs client/src/app/features/roster client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(roster): reject and list a Roster row whose Car is not its Teacher's while the rest imports (#92)"
```

End the commit message with the `Co-Authored-By` trailer from the session's attribution rule.
