# Task 2 of 4: The Roster import never deactivates anyone (backend, migration, client)

> Part of [#92: Roster import stops deactivating Students and enforces that a Student's Car is one of their Teacher's Cars](README.md). Requires task 1 committed. Work on branch `92-roster-car-of-teacher`. Read README decisions 1, 2, 6 and 10 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterResponse.cs`
- Modify: `src\DrivingLessons.Domain\Entities\RosterImport.cs`
- Modify: `src\DrivingLessons.Domain\Events\RosterImportCreated.cs`
- Modify: `src\DrivingLessons.Domain\Values\RosterEntryOutcome.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\RosterImportConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\<timestamp>_RemoveRosterDeactivation.cs` and `.Designer.cs`; Modify: `DrivingLessonsDbContextModelSnapshot.cs` (all generated)
- Modify: `src\DrivingLessons.Application\Queries\GetLatestRosterImport\GetLatestRosterImportResponse.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\RosterImportTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`
- Modify (client): `client\src\app\features\roster\data\import-roster.response.ts`, `data\get-latest-roster-import.response.ts`, `domain\roster-entry-outcome.enum.ts`, `state\roster.store.ts`, `ui\pages\roster\roster.page.html`, `roster.page.ts`, `roster.page.scss`, `ui\components\roster-stat-tile\roster-stat-tile.component.ts`, `roster-stat-tile.component.scss`
- Test (client): `client\src\app\features\roster\state\roster.store.spec.ts`
- Modify: `client\public\i18n\he.json`, `en.json`

**Interfaces:**
- Consumes (task 1): nothing new; `ImportRosterInteractorTest.Init` already assigns `car` to `teacher`.
- Produces (task 3 and the client rely on these):
  - `public record ImportRosterResponse(Guid RosterImportId, int Added, int Updated, int Failed);`
  - `RosterImport` with `AddedCount`, `UpdatedCount`, `FailedCount` (no `DeactivatedCount`).
  - `public record RosterImportCreated(RosterImportId RosterImportId, int AddedCount, int UpdatedCount, int FailedCount, DateTime ImportedAtUtc) : IDomainEvent;`
  - `public enum RosterEntryOutcome { Added = 10, Updated = 20 }`
  - `GetLatestRosterImportResponse` with `Id, FileName, ImportedAtUtc, Added, Updated, Failed, Entries, Failures`.
  - HTTP: `POST api/roster-imports` and `GET api/roster-imports/latest` bodies no longer have `deactivated`; entries' `outcome` is only `added` / `updated`.
  - Client: `ImportRosterResponse { rosterImportId; added; updated; failed }`, `GetLatestRosterImportResponse` without `deactivated`, `RosterEntryOutcome { added, updated }`, `StatTileTone = 'success' | 'info' | 'danger'`.

**Why:** AC 1, 4, 5 (no deactivation of absentees), spec story 57: Students added by hand must survive the next import. README decision 1 removes the count from the whole contract instead of leaving an always-zero field.

- [ ] **Step 1: Rewrite the absentee tests to the new rule**

In `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`, replace `Absent_Student_Is_Deactivated` and `Absent_Inactive_Student_Is_Skipped` with:

```csharp
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
```

Add `using DrivingLessons.Domain.Events;` to the file's usings. `111111118` and `987654324` are valid Israeli national IDs (check digit passes); if `NationalId.Of` rejects one, pick another valid ID with `Faker.FakeNationalId()`'s algorithm and keep it hard-coded.

In `tests\DrivingLessons.Domain.Test\Entities\RosterImportTest.cs`:
- In `Create__Add_Event`, delete the line `&& x.DeactivatedCount == 0`.
- In `Counts_Are_Derived_From_Entries_And_Failures`, delete the entry `RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Deactivated)` (and the comma before it) and the assertion `rosterImport.DeactivatedCount.ShouldBe(1);`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ImportRosterInteractorTest"`
Expected: FAIL in `Absent_Student_Stays_Active` (`student.IsActive should be True but was False`). `Import_Records_Only_Added_And_Updated_Entries` fails too (3 entries: the absentee's `Deactivated` entry).

- [ ] **Step 3: Remove the deactivation step and the count from the import**

`src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ImportRoster;

public record ImportRosterResponse(Guid RosterImportId, int Added, int Updated, int Failed);
```

`src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`:
- Delete the line `DeactivateAbsentees(existingStudents, seenIds, entries);` (and the blank line after it).
- Delete the whole `private static void DeactivateAbsentees(...)` method.
- Return the response without the count:

```csharp
        return new ImportRosterResponse(
            rosterImport.Id.Value,
            rosterImport.AddedCount,
            rosterImport.UpdatedCount,
            rosterImport.FailedCount);
```

`existingStudents`, `studentsByNationalId` and `seenIds` stay: upsert and duplicate detection still use them. The reactivation of a reappearing Inactive Student inside `ProcessRow` stays (README decision 6).

`src\DrivingLessons.Domain\Values\RosterEntryOutcome.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum RosterEntryOutcome
{
    Added = 10,
    Updated = 20
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
    int FailedCount,
    DateTime ImportedAtUtc) : IDomainEvent;
```

`src\DrivingLessons.Domain\Entities\RosterImport.cs`: delete the `DeactivatedCount` property and the line `DeactivatedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Deactivated);`, and build the event without it:

```csharp
        var createdEvent = new RosterImportCreated(
            id,
            AddedCount,
            UpdatedCount,
            FailedCount,
            importedAtUtc);
```

`src\DrivingLessons.Application\Queries\GetLatestRosterImport\GetLatestRosterImportResponse.cs`: delete `public int Deactivated { get; init; }` and the selector line `Deactivated = x.DeactivatedCount,`.

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\RosterImportConfiguration.cs`: delete

```csharp
        builder
            .Property(x => x.DeactivatedCount)
            .HasColumnName("deactivated_count");

```

- [ ] **Step 4: Prove nothing else used the removed members**

```bash
git grep -n "DeactivatedCount\|RosterEntryOutcome.Deactivated\|\.Deactivated\b" -- src tests
```

Expected: only hits inside `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\` (old migrations and the snapshot, which Step 6 regenerates). No handler subscribes to `RosterImportCreated` (`git grep -n "RosterImportCreated" -- src` shows only the event and `RosterImport.cs`). If anything else shows up, stop and report it: README decision 1 assumed nothing consumes the field.

- [ ] **Step 5: Run the backend suites**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: PASS.

- [ ] **Step 6: Generate the migration and add the data cleanup**

```bash
dotnet ef migrations add RemoveRosterDeactivation --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web --output-dir EntityFramework\Migrations
```

Expected: a new `<timestamp>_RemoveRosterDeactivation.cs` whose `Up` has one `DropColumn(name: "deactivated_count", table: "roster_imports")` and whose `Down` has the matching `AddColumn<int>` (`nullable: false, defaultValue: 0`), plus an updated snapshot. If it contains anything else, the model changed somewhere unintended: stop and fix the model instead of editing the migration.

Then make `Up` delete the historical entries with the removed outcome before dropping the column (README decision 2; `outcome` is an `integer` column, `30` was `Deactivated`):

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM roster_import_entries WHERE outcome = 30;");

            migrationBuilder.DropColumn(
                name: "deactivated_count",
                table: "roster_imports");
        }
```

Leave `Down` as generated.

- [ ] **Step 7: Check the model and the migration**

```bash
dotnet build
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: build succeeds; `No changes have been made to the model since the last migration.` Task 4 applies the migration to a database that already holds a deactivating import.

- [ ] **Step 8: Write the failing client spec**

Add to `client\src\app\features\roster\state\roster.store.spec.ts` (keep the existing test). Add the imports `of` (from `rxjs`, next to `throwError`), `GetLatestRosterImportResponse` (`../data/get-latest-roster-import.response`) and `RosterEntryOutcome` (`../domain/roster-entry-outcome.enum`), then:

```ts
const LATEST_IMPORT: GetLatestRosterImportResponse = {
    id: 'import-1',
    fileName: 'roster.csv',
    importedAtUtc: '2026-10-05T08:00:00Z',
    added: 1,
    updated: 1,
    failed: 0,
    entries: [
        { nationalId: '123456782', outcome: RosterEntryOutcome.added },
        { nationalId: '987654324', outcome: RosterEntryOutcome.updated },
    ],
    failures: [],
};

function storeWithLatestImport(latestImport: GetLatestRosterImportResponse): RosterStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: RosterApiService, useValue: { getLatestImport: () => of(latestImport), findStudents: () => of([]) } },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    return TestBed.inject(RosterStore);
}
```

and inside `describe('RosterStore', ...)`:

```ts
    it('badges every Student the latest import added or updated', async () => {
        //given
        const store = storeWithLatestImport(LATEST_IMPORT);

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.badgeByNationalId()).toEqual(
            new Map([
                ['123456782', RosterEntryOutcome.added],
                ['987654324', RosterEntryOutcome.updated],
            ]),
        );
    });
```

- [ ] **Step 9: Run it to see it fail**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/roster/state/roster.store.spec.ts
```

Expected: FAIL with a TypeScript error: `Property 'deactivated' is missing in type ... but required in type 'GetLatestRosterImportResponse'`.

- [ ] **Step 10: Remove `deactivated` from the client**

`client\src\app\features\roster\data\import-roster.response.ts`:

```ts
export interface ImportRosterResponse {
    rosterImportId: string;
    added: number;
    updated: number;
    failed: number;
}
```

`client\src\app\features\roster\data\get-latest-roster-import.response.ts`: delete the line `deactivated: number;`.

`client\src\app\features\roster\domain\roster-entry-outcome.enum.ts`:

```ts
export enum RosterEntryOutcome {
    added = 'added',
    updated = 'updated',
}
```

`client\src\app\features\roster\state\roster.store.ts`: every entry now gets a badge:

```ts
    readonly badgeByNationalId = computed<Map<string, RosterEntryOutcome>>(() => {
        const badges = new Map<string, RosterEntryOutcome>();

        for (const entry of this.latestImport()?.entries ?? []) {
            badges.set(entry.nationalId, entry.outcome);
        }

        return badges;
    });
```

`client\src\app\features\roster\ui\pages\roster\roster.page.html`: delete the third tile

```html
                <app-roster-stat-tile
                    [value]="latestImport.deactivated"
                    [label]="'roster.stats.deactivated' | transloco"
                    tone="neutral" />
```

`client\src\app\features\roster\ui\pages\roster\roster.page.ts`:

```ts
    protected readonly processedRows = computed(() => {
        const latest = this.store.latestImport();

        return latest ? latest.added + latest.updated + latest.failed : 0;
    });
```

`client\src\app\features\roster\ui\pages\roster\roster.page.scss`: the stats grid is three equal columns at every width (README decision 10). Change `.roster__stats { grid-template-columns: repeat(4, 1fr); ... }` to `repeat(3, 1fr)`, and delete the narrow-screen override block

```scss
    .roster__stats {
        grid-template-columns: repeat(2, 1fr);
    }

```

from the media query (leave the media query's other rules).

`client\src\app\features\roster\ui\components\roster-stat-tile\roster-stat-tile.component.ts`:

```ts
export type StatTileTone = 'success' | 'info' | 'danger';
```

`client\src\app\features\roster\ui\components\roster-stat-tile\roster-stat-tile.component.scss`: delete the `.stat-tile--neutral .stat-tile__value { ... }` rule.

`client\public\i18n\he.json` and `en.json`: delete `roster.stats.deactivated` (he: "הושבתו (חסרים בקובץ)", en: "Deactivated (missing from file)"). Keep `roster.inactive` ("לא פעיל" / "Inactive"): Students deactivated by earlier imports are still Inactive Students and keep their tag.

- [ ] **Step 11: Run every client check**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec passes (including the new badge test and `translations.spec.ts`); the build succeeds. Then confirm no leftover reference:

```bash
git grep -n -i "deactivated" -- client/src client/public
```

Expected: no hit in `features/roster` or `roster.*` translation keys (hits elsewhere, e.g. users or student-form, are unrelated and stay).

- [ ] **Step 12: Commit**

```bash
git add src/DrivingLessons.Application/Commands/ImportRoster src/DrivingLessons.Application/Queries/GetLatestRosterImport src/DrivingLessons.Domain/Entities/RosterImport.cs src/DrivingLessons.Domain/Events/RosterImportCreated.cs src/DrivingLessons.Domain/Values/RosterEntryOutcome.cs src/DrivingLessons.Infrastructure/EntityFramework tests/DrivingLessons.Domain.Test/Entities/RosterImportTest.cs tests/DrivingLessons.Application.Test/Commands/ImportRosterInteractorTest.cs client/src/app/features/roster client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(roster): the Roster import never deactivates Students and drops the deactivated count (#92)"
```

End the commit message with the `Co-Authored-By` trailer from the session's attribution rule.
