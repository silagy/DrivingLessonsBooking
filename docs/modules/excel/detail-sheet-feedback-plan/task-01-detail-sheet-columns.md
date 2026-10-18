# Task 1 of 2: Car name and requested lessons in, National ID, Phone and Transmission out

> Part of [Detail Sheet Customer Feedback](README.md). Read its Decisions and Global Constraints first. Branch `customer-feedback-excel-and-target`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` (`SlotRequestDetail`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` (`GetSlotRequestDetailsAsync`, lines 46-89)
- Modify: `src\DrivingLessons.Infrastructure\Excel\HebrewExcelLabels.cs`
- Modify: `src\DrivingLessons.Infrastructure\Excel\RequestDetailSheet.cs`
- Test: `tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs`, `ExcelGeneratorTest.cs`, `ExcelTypographyTest.cs`
- Docs: `docs\requirements.md` §9 Sheet 2, `docs\modules\excel\README.md`, `docs\decisions\0003-roster-csv-and-weekly-link-model.md`

**Interfaces:**
- Consumes: `Car.Name` (`CarName`, `.Value`), `Submission.SlotRequests`, `Submission.TargetCount` (`TargetSessionCount`, `.Value`).
- Produces:
  - `public record SlotRequestDetail(DayOfWeek Day, SlotWindowType Window, string StudentName, string CarName, SessionType SessionType, int Rank, int TargetCount, int PickCount, string? Constraint)`.
  - `HebrewExcelLabels.RequestedLessonsOf(int targetCount, int pickCount) : string` → `"{targetCount} מתוך {pickCount}"`.
  - `RequestDetailSheet.AddTo(XLWorkbook, IReadOnlyCollection<SlotRequestDetail>)`: same signature, eight columns.

- [ ] **Step 1: Rewrite the sheet tests for the eight columns**

In `RequestDetailSheetTest.cs`:
- `ColumnCount = 8`, `ConstraintsColumn = 8`; add `CarColumn = 4`, `RequestedLessonsColumn = 7`.
- The `Request(...)` helper takes `string carName = "Smoke Auto"` and `int pickCount = 1` and drops `nationalId`, `phone`, `transmission`; it builds the record in the Interfaces order.
- `Writes_The_Hebrew_Header_Row` expects exactly `["יום", "משבצת", "שם התלמיד/ה", "רכב", "סוג שיעור", "דירוג", "שיעורים מבוקשים", "אילוצים"]`.
- `Writes_Every_Booking_Field_Of_A_Slot_Request` (Tuesday, Afternoon, rank 2, `"דנה כהן"`, car `"Smoke Manual"`, Double, target 3, picks 5, `"רק אחרי 16:00"`) expects row 2 `["שלישי", "אחה״צ", "דנה כהן", "Smoke Manual", "כפול", "2", "3 מתוך 5", "רק אחרי 16:00"]`, cell (2, 6) `XLDataType.Number` and cell (2, 7) `XLDataType.Text`.
- `Sorts_By_Day_Then_Slot_Then_Rank` reads the rank from `row[5]`.
- Rename `Orders_Students_With_The_Same_Slot_And_Rank_By_Name` to `Orders_Students_With_The_Same_Slot_And_Rank_By_Name_Then_Car`. Requests: (`"דנה כהן"`, `"Smoke Old"`), (`"Smoke Student A"`, `"Smoke Manual"`), (`"Smoke Student A"`, `"Smoke Auto"`). Expected `$"{row[2]} {row[3]}"` order: `"Smoke Student A Smoke Auto"`, `"Smoke Student A Smoke Manual"`, `"דנה כהן Smoke Old"`.
- Delete `Keeps_National_Id_And_Phone_As_Text` and `Names_The_Transmission_In_Hebrew`.
- `Is_Right_To_Left_With_A_Frozen_Filtered_Header` expects the AutoFilter range `"A1:H3"`.
- `Names_The_Session_Type_In_Hebrew` reads column 5.

Add:

```csharp
[TestMethod]
[DataRow(1, 1, "1 מתוך 1")]
[DataRow(1, 3, "1 מתוך 3")]
[DataRow(2, 5, "2 מתוך 5")]
public void Writes_The_Requested_Lessons_As_Target_Out_Of_Picks(int targetCount, int pickCount, string expected)
    // AddTo with Request(targetCount: targetCount, pickCount: pickCount)
    // then Reloaded(workbook).Cell(2, RequestedLessonsColumn).GetText().ShouldBe(expected)

[TestMethod]
[DataRow("12-345-67")]
[DataRow("0501234")]
[DataRow("=A1")]
public void Keeps_A_Car_Name_That_Looks_Like_A_Number_Or_A_Formula_As_Text(string carName)
    // cell (2, CarColumn): HasFormula false, DataType XLDataType.Text, GetText() == carName
```

In `ExcelGeneratorTest.cs` and `ExcelTypographyTest.cs`, construct `SlotRequestDetail` in the new argument order, using car `"Smoke Auto"` and picks `2` (generator) / `3` (typography). `Detail_Sheet_Lists_The_Requests_Of_The_Teacher_And_Publication` asserts `detail.Cell(2, 4).GetText().ShouldBe("Smoke Auto")` in place of the national ID.

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: FAIL. The build breaks on `SlotRequestDetail`'s constructor (`CS1503` / `CS7036`).

- [ ] **Step 3: Change the read model and the query**

- `SlotRequestDetail` takes the Interfaces signature.
- In `GetSlotRequestDetailsAsync`, keep the joins and the `IgnoreQueryFilters([QueryFilters.CarNotDeleted])` car join. Project `CarName = car.Name` and `PickCount = submission.SlotRequests.Count()`, and drop `student.NationalId`, `student.Phone`, `car.Transmission`. Map `x.CarName.Value` and `x.PickCount`.

If EF Core cannot translate `submission.SlotRequests.Count()` next to the `from request in submission.SlotRequests` expansion, load the rows without it. Then fill `PickCount` in memory from a second query, `TeacherSubmissions(...).Select(s => new { s.Id, Count = s.SlotRequests.Count() })`, keyed by submission id (add `SubmissionId = submission.Id` to the first projection). Task 2's smoke is the proof either way.

- [ ] **Step 4: Change the labels and the sheet**

- `HebrewExcelLabels`: delete `TransmissionOf`; add `RequestedLessonsOf` (Interfaces).
- `RequestDetailSheet`: the eight headers from Decision 1; widths `[8, 10, 24, 18, 10, 7, 14, 40]`; `ConstraintsColumn = 8`; delete `NationalIdColumn`, `PhoneColumn`, `TextFormat` and their two `NumberFormat` lines. The order is `Day, Window, Rank, StudentName (Ordinal), CarName (Ordinal)`. The row values are `DayOf, WindowOf, StudentName, CarName, SessionTypeOf, Rank, RequestedLessonsOf(TargetCount, PickCount), ConstraintOf(Constraint)`.

- [ ] **Step 5: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: PASS, including `ExcelTypographyTest` and `SummarySheetTest`.

Then `dotnet build` and `dotnet test`. Expected: no new warnings beyond the existing `NU1903` / `MSTEST0001` / `CS8618`, and every test passes. Also run `rg -n "TransmissionOf|NationalIdColumn|PhoneColumn" src tests`. Expected: no matches.

- [ ] **Step 6: The docs**

- `docs\requirements.md` §9 Sheet 2: the table becomes Day, Slot, Student name, Car (the student's roster car name), Session type, Rank, Requested lessons ("target out of picks", for example 1 out of 3), Constraints. Add one line under it: "National ID, phone and transmission were removed at the customer's request (October 2026)."
- `docs\modules\excel\README.md`: add slice row `4. Customer feedback ([plan](detail-sheet-feedback-plan/README.md))`, and add locked decision 7: "Sheet 2 carries the car name and `{target} מתוך {picks}`; no national ID, phone or transmission (customer feedback, 18 October 2026)." Decision 5's "transmission comes from the car" becomes "the car name comes from the student's current roster car, including a soft-deleted one".
- ADR 0003: append `## Amendment (October 2026)` with one sentence: at the customer's request, the Excel detail sheet no longer carries National ID or Phone; the student is identified there by name and car. The national ID remains the student-form identifier.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Queries/ISubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/EntityFramework/Queries/SubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/Excel/HebrewExcelLabels.cs \
  src/DrivingLessons.Infrastructure/Excel/RequestDetailSheet.cs \
  tests/DrivingLessons.Application.Test/Excel \
  docs/requirements.md docs/modules/excel docs/decisions/0003-roster-csv-and-weekly-link-model.md
git commit -m "feat(excel): detail sheet shows the car and lessons requested out of picks, without ID, phone or transmission"
```

---

**Next:** [task-02-download-smoke.md](task-02-download-smoke.md)
