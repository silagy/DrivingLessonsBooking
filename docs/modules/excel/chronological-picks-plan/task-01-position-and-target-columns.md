# Task 1 of 2: Position and target columns in, rank out

> Part of [Chronological Picks](README.md). Read its Decisions and Global Constraints first. Branch `excel-chronological-picks`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` (`SlotRequestDetail`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` (`GetSlotRequestDetailsAsync`, lines 46-87)
- Modify: `src\DrivingLessons.Infrastructure\Excel\HebrewExcelLabels.cs`
- Modify: `src\DrivingLessons.Infrastructure\Excel\RequestDetailSheet.cs`
- Test: `tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs`, `ExcelGeneratorTest.cs`, `ExcelTypographyTest.cs`
- Docs: `docs\requirements.md` §9 Sheet 2, `docs\modules\excel\README.md`

**Interfaces:**
- Consumes: `Submission.Id` (`SubmissionId`, `.Value`), `Submission.SlotRequests`, `Submission.TargetCount` (`.Value`), `Car.Name` (`.Value`).
- Produces:
  - `public record SlotRequestDetail(Guid SubmissionId, DayOfWeek Day, SlotWindowType Window, string StudentName, string CarName, SessionType SessionType, int TargetCount, int PickCount, string? Constraint)`.
  - `HebrewExcelLabels.PickPositionOf(int position, int pickCount) : string` → `"{position} מתוך {pickCount}"`.
  - `RequestDetailSheet.AddTo(XLWorkbook, IReadOnlyCollection<SlotRequestDetail>)`: same signature, the eight columns of Decision 1.

- [ ] **Step 1: Rewrite the sheet tests for the new columns**

In `RequestDetailSheetTest.cs`:
- Constants: `ColumnCount = 8`, `CarColumn = 4`, `PositionColumn = 6`, `TargetCountColumn = 7`, `ConstraintsColumn = 8`. `RequestedLessonsColumn` goes.
- The `Request(...)` helper drops `rank` and gains a first parameter `Guid? submissionId = null`; it builds the record in the Interfaces order with `submissionId ?? Guid.NewGuid()`, so every request a test doesn't group is its own submission.
- `Writes_The_Hebrew_Header_Row` expects exactly `["יום", "משבצת", "שם התלמיד/ה", "רכב", "סוג שיעור", "שיעור אפשרי", "שיעורים מבוקשים", "אילוצים"]`.
- `Writes_Every_Booking_Field_Of_A_Slot_Request` (Tuesday, Afternoon, `"דנה כהן"`, car `"Smoke Manual"`, Double, target 3, picks 5, `"רק אחרי 16:00"`, no rank) expects row 2 `["שלישי", "אחה״צ", "דנה כהן", "Smoke Manual", "כפול", "1 מתוך 5", "3", "רק אחרי 16:00"]`, cell (2, `PositionColumn`) `XLDataType.Text` and cell (2, `TargetCountColumn`) `XLDataType.Number`.
- `Sorts_By_Day_Then_Slot_Then_Rank` becomes `Sorts_By_Day_Then_Slot`: requests Friday Noon, Sunday Evening, Sunday Morning, Monday Morning; `$"{row[0]} {row[1]}"` is `"ראשון בוקר"`, `"ראשון ערב"`, `"שני בוקר"`, `"שישי צהריים"`.
- `Orders_Students_With_The_Same_Slot_And_Rank_By_Name_Then_Car` becomes `Orders_Students_In_The_Same_Slot_By_Name_Then_Car`, same data and expectation.
- Delete `Writes_The_Requested_Lessons_As_Target_Out_Of_Picks`.

Add:

```csharp
[TestMethod]
public void Numbers_A_Students_Slots_From_Sunday_Morning_To_Friday_Evening()
    // one submissionId, pickCount 4, handed in as Friday Noon, Sunday Evening, Monday Morning, Sunday Morning
    // $"{row[0]} {row[1]} {row[5]}" is
    //   "ראשון בוקר 1 מתוך 4", "ראשון ערב 2 מתוך 4", "שני בוקר 3 מתוך 4", "שישי צהריים 4 מתוך 4"

[TestMethod]
public void Numbers_Each_Submission_On_Its_Own_Even_With_The_Same_Name()
    // both "דנה כהן", car "Smoke Auto"
    // submission X (pickCount 2): Monday Morning, Sunday Morning; submission Y (pickCount 1): Sunday Evening
    // $"{row[0]} {row[1]} {row[5]}" is
    //   "ראשון בוקר 1 מתוך 2", "ראשון ערב 1 מתוך 1", "שני בוקר 2 מתוך 2"

[TestMethod]
[DataRow(1)]
[DataRow(3)]
public void Writes_The_Target_Count_As_A_Number(int targetCount)
    // cell (2, TargetCountColumn): DataType XLDataType.Number, GetValue<int>() == targetCount
```

In `ExcelGeneratorTest.cs` and `ExcelTypographyTest.cs`, construct `SlotRequestDetail` in the new order: `Guid.NewGuid()` first, the rank argument gone, everything else as today.

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: FAIL. The build breaks on `SlotRequestDetail`'s constructor (`CS1503` / `CS7036`).

- [ ] **Step 3: Change the read model and the query**

- `SlotRequestDetail` takes the Interfaces signature.
- In `GetSlotRequestDetailsAsync`, keep every join (the `IgnoreQueryFilters([QueryFilters.CarNotDeleted])` car join included) and `PickCount = submission.SlotRequests.Count()`. Project `SubmissionId = submission.Id` in place of `request.Rank`, and map `x.SubmissionId.Value` first.

- [ ] **Step 4: Change the labels and the sheet**

- `HebrewExcelLabels`: rename `RequestedLessonsOf` to `PickPositionOf` (Interfaces).
- `RequestDetailSheet`: the headers and widths of Decision 1. In `WriteRequests`, number each submission's rows first (group by `SubmissionId`, order each group by `Day` then `Window`, position = index + 1), then write all rows ordered by `Day`, `Window`, `StudentName` (Ordinal), `CarName` (Ordinal), `SubmissionId`. `WriteRequest` takes the position; its values are `DayOf, WindowOf, StudentName, CarName, SessionTypeOf, PickPositionOf(position, PickCount), TargetCount, ConstraintOf(Constraint)`.

- [ ] **Step 5: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: PASS, including `ExcelTypographyTest` and `SummarySheetTest`.

Then `dotnet build` and `dotnet test`. Expected: no new warnings beyond the existing `NU1903` / `MSTEST0001` / `CS8618`, and every test passes. Also run `rg -n "RequestedLessonsOf|Rank" src/DrivingLessons.Infrastructure/Excel tests/DrivingLessons.Application.Test/Excel`. Expected: no matches.

- [ ] **Step 6: The docs**

- `docs\requirements.md` §9 Sheet 2: the lead line reads "sorted by day, then slot, then student name". The Rank row goes. After Session type, add `| Slot position | The slot's place among the student's picks, counted from Sunday morning to Friday evening, for example "2 out of 8" (`2 מתוך 8`) |`. Requested lessons becomes "The student's weekly target, as a number". Under the national-ID line add: "Rank was removed at the customer's request (October 2026); students still rank their picks on the form."
- `docs\modules\excel\README.md`: add locked decision 8: "Sheet 2 numbers each student's slots `{position} מתוך {picks}` from Sunday morning to Friday evening, shows the target count as a number, and carries no rank (customer feedback, 26 October 2026; slice 5)." Decision 7 ends with "Its `{target} מתוך {picks}` column was replaced by decision 8."

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Queries/ISubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/EntityFramework/Queries/SubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/Excel/HebrewExcelLabels.cs \
  src/DrivingLessons.Infrastructure/Excel/RequestDetailSheet.cs \
  tests/DrivingLessons.Application.Test/Excel \
  docs/requirements.md docs/modules/excel
git commit -m "feat(excel): detail sheet numbers each student's slots from Sunday morning and shows the target, without rank"
```

---

**Next:** [task-02-download-smoke-and-pr.md](task-02-download-smoke-and-pr.md)
