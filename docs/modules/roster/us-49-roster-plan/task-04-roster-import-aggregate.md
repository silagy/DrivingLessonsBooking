# Task 4 of 10: RosterImport aggregate

> Part of [US-49: Roster Module](README.md). Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\RosterImportId.cs`, `RosterFileName.cs`, `RosterImportEntry.cs`, `RosterImportFailure.cs`, `RosterEntryOutcome.cs`, `RosterRowFailureReason.cs`
- Create: `src\DrivingLessons.Domain\Entities\RosterImport.cs`
- Create: `src\DrivingLessons.Domain\Events\RosterImportCreated.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\RosterFileNameMustNotBeEmptyException.cs`, `RosterImportFailureRowNumberMustBePositiveException.cs`, `RosterFileMustContainRequiredColumnsException.cs`, `RosterFileMustNotBeEmptyException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\RosterImportTest.cs`, `tests\DrivingLessons.Domain.Test\Entities\Fake\RosterImportFakeBuilder.cs`, `tests\DrivingLessons.Domain.Test\Values\RosterImportFailureTest.cs`, `RosterFileNameTest.cs`
- Modify: `tests\DrivingLessons.Domain.Test\Common\Faker.cs`

Before coding, open `src\DrivingLessons.Domain\Entities\Car.cs` (backing `List` field + `AsReadOnly()` collection property), `src\DrivingLessons.Domain\Values\SlotState.cs` (int-valued enum) and `src\DrivingLessons.Domain\Values\TeacherId.cs`/`TeacherName.cs` and match their exact shapes.

Design notes (planning decisions 4, 6, 12):

- **`RosterImport` is a create-only aggregate** — one persisted record per upload (filename, timestamp, counts, entries, failed rows) so the roster page's last-import summary survives refresh. No transitions, no `MustBe*` guards.
- **Counts are COMPUTED from the collections at creation** (single source of truth) — `Create` never accepts counts as parameters.
- **`RosterRowFailureReason` is an enum, never backend free text** — the client translates it (i18n rule 11).
- **`RosterImportFailure.StudentName` stays a raw nullable `string` deliberately** — it echoes unvalidated CSV text for the failed-rows panel; wrapping it in `StudentName` would reject the very rows being reported.
- **The two file-level exceptions** (`RosterFileMustContainRequiredColumnsException`, `RosterFileMustNotBeEmptyException`) are defined now but thrown later by the CSV parser ([task 5](task-05-csv-parser.md)) — whole-file errors map to 409 via the existing exception filter.

- [ ] **Step 1: Write failing tests**

Add to `tests\DrivingLessons.Domain.Test\Common\Faker.cs` (inside the existing class):

```csharp
public static DateTime FakeUtcDate()
{
    var minutesAhead = Random.Shared.Next(1, 100000);

    return DateTime.UtcNow.AddMinutes(minutesAhead);
}
```

`tests\DrivingLessons.Domain.Test\Values\RosterFileNameTest.cs` — copy `TeacherNameTest.cs` verbatim, rename `TeacherName` → `RosterFileName`, `TeacherNameMustNotBeEmptyException` → `RosterFileNameMustNotBeEmptyException`, and the test methods `Name_Is_Trimmed` → `File_Name_Is_Trimmed`, `Name_Must_Not_Be_Empty` → `File_Name_Must_Not_Be_Empty`.

`tests\DrivingLessons.Domain.Test\Values\RosterImportFailureTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class RosterImportFailureTest
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Row_Number_Must_Be_Positive(int rowNumber)
    {
        //given
        var studentName = Faker.FakeString();

        //when
        var act = () => RosterImportFailure.Of(rowNumber, studentName, RosterRowFailureReason.InvalidNationalId);

        //then
        Should.Throw<RosterImportFailureRowNumberMustBePositiveException>(act);
    }
}
```

`tests\DrivingLessons.Domain.Test\Entities\Fake\RosterImportFakeBuilder.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class RosterImportFakeBuilder
{
    public static RosterImport Build()
    {
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entry = RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added);
        var failure = RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId);

        return RosterImport.Create(fileName, importedAtUtc, [entry], [failure]);
    }
}
```

`tests\DrivingLessons.Domain.Test\Entities\RosterImportTest.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class RosterImportTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entry = RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added);
        var failure = RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId);

        //when
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, [entry], [failure]);

        //then
        rosterImport.FileName.ShouldBe(fileName);
        rosterImport.ImportedAtUtc.ShouldBe(importedAtUtc);
        rosterImport.Entries.ShouldContain(entry);
        rosterImport.Failures.ShouldContain(failure);
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entry = RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added);
        var failure = RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId);

        //when
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, [entry], [failure]);

        //then
        rosterImport
            .UncommittedEvents
            .OfType<RosterImportCreated>()
            .Where(x => x.RosterImportId == rosterImport.Id
                        && x.AddedCount == 1
                        && x.UpdatedCount == 0
                        && x.DeactivatedCount == 0
                        && x.FailedCount == 1
                        && x.ImportedAtUtc == importedAtUtc)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Counts_Are_Derived_From_Entries_And_Failures()
    {
        //given
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entries = new List<RosterImportEntry>
        {
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added),
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added),
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Updated),
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Deactivated)
        };
        var failures = new List<RosterImportFailure>
        {
            RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId),
            RosterImportFailure.Of(2, null, RosterRowFailureReason.UnknownTeacher)
        };

        //when
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, entries, failures);

        //then
        rosterImport.AddedCount.ShouldBe(2);
        rosterImport.UpdatedCount.ShouldBe(1);
        rosterImport.DeactivatedCount.ShouldBe(1);
        rosterImport.FailedCount.ShouldBe(2);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL (compilation error) — `RosterImport` and its value objects, event and exceptions do not exist.

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Values\RosterImportId.cs` — copy `TeacherId.cs` verbatim, rename `TeacherId` → `RosterImportId`.

`src\DrivingLessons.Domain\Values\RosterFileName.cs` — copy `TeacherName.cs` verbatim, rename `TeacherName` → `RosterFileName` and `TeacherNameMustNotBeEmptyException` → `RosterFileNameMustNotBeEmptyException`.

`src\DrivingLessons.Domain\Values\RosterEntryOutcome.cs` (int-valued like `SlotState`):

```csharp
namespace DrivingLessons.Domain.Values;

public enum RosterEntryOutcome
{
    Added = 10,
    Updated = 20,
    Deactivated = 30
}
```

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
    InvalidStartDate = 90
}
```

`src\DrivingLessons.Domain\Values\RosterImportEntry.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public record RosterImportEntry
{
    public NationalId NationalId { get; }
    public RosterEntryOutcome Outcome { get; }

    private RosterImportEntry(NationalId nationalId, RosterEntryOutcome outcome)
    {
        NationalId = nationalId;
        Outcome = outcome;
    }

    public static RosterImportEntry Of(NationalId nationalId, RosterEntryOutcome outcome)
    {
        return new RosterImportEntry(nationalId, outcome);
    }
}
```

`src\DrivingLessons.Domain\Values\RosterImportFailure.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record RosterImportFailure
{
    public int RowNumber { get; }
    public string? StudentName { get; }
    public RosterRowFailureReason Reason { get; }

    private RosterImportFailure(int rowNumber, string? studentName, RosterRowFailureReason reason)
    {
        RowNumber = rowNumber;
        StudentName = studentName;
        Reason = reason;
    }

    public static RosterImportFailure Of(int rowNumber, string? studentName, RosterRowFailureReason reason)
    {
        if (rowNumber < 1)
        {
            throw new RosterImportFailureRowNumberMustBePositiveException(rowNumber);
        }

        return new RosterImportFailure(rowNumber, studentName, reason);
    }
}
```

`src\DrivingLessons.Domain\Entities\RosterImport.cs` (backing `List` fields + `AsReadOnly()` like `Car.TeacherAssignments`):

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class RosterImport : AggregateRoot<RosterImportId>
{
    private readonly List<RosterImportEntry> entries = [];
    private readonly List<RosterImportFailure> failures = [];

    public RosterFileName FileName { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
    public int AddedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int DeactivatedCount { get; private set; }
    public int FailedCount { get; private set; }

    public IReadOnlyCollection<RosterImportEntry> Entries => entries.AsReadOnly();
    public IReadOnlyCollection<RosterImportFailure> Failures => failures.AsReadOnly();

    private RosterImport()
    {
    }

    private RosterImport(
        RosterImportId id,
        RosterFileName fileName,
        DateTime importedAtUtc,
        IReadOnlyCollection<RosterImportEntry> entries,
        IReadOnlyCollection<RosterImportFailure> failures)
        : base(id)
    {
        FileName = fileName;
        ImportedAtUtc = importedAtUtc;
        this.entries.AddRange(entries);
        this.failures.AddRange(failures);
        AddedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Added);
        UpdatedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Updated);
        DeactivatedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Deactivated);
        FailedCount = failures.Count;

        var createdEvent = new RosterImportCreated(
            id,
            AddedCount,
            UpdatedCount,
            DeactivatedCount,
            FailedCount,
            importedAtUtc);
        AddEvent(createdEvent);
    }

    public static RosterImport Create(
        RosterFileName fileName,
        DateTime importedAtUtc,
        IReadOnlyCollection<RosterImportEntry> entries,
        IReadOnlyCollection<RosterImportFailure> failures)
    {
        var id = RosterImportId.New();

        return new RosterImport(id, fileName, importedAtUtc, entries, failures);
    }
}
```

`src\DrivingLessons.Domain\Events\RosterImportCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record RosterImportCreated(
    RosterImportId RosterImportId,
    int AddedCount,
    int UpdatedCount,
    int DeactivatedCount,
    int FailedCount,
    DateTime ImportedAtUtc) : IDomainEvent;
```

`src\DrivingLessons.Domain\Exceptions\RosterFileNameMustNotBeEmptyException.cs` — copy `TeacherNameMustNotBeEmptyException.cs` verbatim, rename the class and change the message to `"Roster file name must not be empty."`.

`src\DrivingLessons.Domain\Exceptions\RosterImportFailureRowNumberMustBePositiveException.cs` (a row number is not PII, so it may appear in the message):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterImportFailureRowNumberMustBePositiveException : DomainException
{
    public RosterImportFailureRowNumberMustBePositiveException(int rowNumber)
        : base($"Roster import failure row number {rowNumber} must be positive.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\RosterFileMustContainRequiredColumnsException.cs` (thrown by the CSV parser in task 5; header names are not PII):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileMustContainRequiredColumnsException : DomainException
{
    public RosterFileMustContainRequiredColumnsException(IReadOnlyCollection<string> missingColumns)
        : base(BuildMessage(missingColumns))
    {
    }

    private static string BuildMessage(IReadOnlyCollection<string> missingColumns)
    {
        var columns = string.Join(", ", missingColumns);

        return $"Roster file must contain required columns: {columns}.";
    }
}
```

`src\DrivingLessons.Domain\Exceptions\RosterFileMustNotBeEmptyException.cs` (thrown by the CSV parser in task 5):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileMustNotBeEmptyException : DomainException
{
    public RosterFileMustNotBeEmptyException()
        : base("Roster file must contain at least one student row.")
    {
    }
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS (all new + existing tests green).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): add RosterImport aggregate recording import outcomes"
```

---

**Next:** [task-05-csv-parser.md](task-05-csv-parser.md)
