# Task 3 of 4: One detail sheet per car

> Part of [Sheet per Car](README.md). Requires tasks 1-2 committed. Branch `excel-sheet-per-car`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\Excel\RequestDetailSheet.cs`
- Modify: `src\DrivingLessons.Infrastructure\Excel\ExcelGenerator.cs`
- Test: `tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs`, `ExcelGeneratorTest.cs`
- Docs: `docs\requirements.md` §9, `docs\modules\excel\README.md`, any other doc that still says the file has two sheets (`rg -n "Two sheets|two-sheet|פירוט בקשות" docs`; leave earlier slices' plan folders as history)

**Interfaces:**
- Consumes: `CarSheetNames.For` (task 1), `SlotRequestDetail.CarId` (task 2).
- Produces: `RequestDetailSheet.AddTo(XLWorkbook workbook, string sheetName, string carName, IReadOnlyCollection<SlotRequestDetail> requests)` (Decision 12); `ExcelGenerator.GenerateAsync` unchanged in signature, workbook per Decisions 1-4.

- [ ] **Step 1: Rewrite the sheet tests for a car's sheet**

In `RequestDetailSheetTest.cs`:
- Constants: `ColumnCount = 7`, `TitleRow = 1`, `HeaderRow = 2`, `FirstRequestRow = 3`, `SessionTypeColumn = 4`, `PositionColumn = 5`, `TargetCountColumn = 6`, `ConstraintsColumn = 7`, `SheetName = "Smoke Auto"`, `CarName = "Smoke Auto"`. `CarColumn` goes.
- A helper `AddTo(XLWorkbook workbook, params SlotRequestDetail[] requests)` calls `RequestDetailSheet.AddTo(workbook, SheetName, CarName, requests)`; `Reloaded` opens `Worksheet(SheetName)`; `RequestRowsOf` starts at `FirstRequestRow`.
- `Writes_The_Hebrew_Header_Row` reads row `HeaderRow` and expects exactly `["יום", "משבצת", "שם התלמיד/ה", "סוג שיעור", "שיעור אפשרי", "שיעורים מבוקשים", "אילוצים"]`.
- `Lists_Only_The_Header_When_There_Are_No_Requests` becomes `Lists_Only_The_Title_And_Header_When_There_Are_No_Requests`: last used row is 2.
- `Writes_Every_Booking_Field_Of_A_Slot_Request` expects row 3 `["שלישי", "אחה״צ", "דנה כהן", "כפול", "1 מתוך 5", "3", "רק אחרי 16:00"]`.
- `Orders_Students_In_The_Same_Slot_By_Name_Then_Car` becomes `Orders_Students_In_The_Same_Slot_By_Name`: `"דנה כהן"`, `"Smoke Student B"`, `"Smoke Student A"` come out `"Smoke Student A"`, `"Smoke Student B"`, `"דנה כהן"`.
- `Keeps_A_Car_Name_That_Looks_Like_A_Number_Or_A_Formula_As_Text` now checks the title cell (1, 1), calling `RequestDetailSheet.AddTo(workbook, SheetName, carName, [Request()])`.
- `Is_Right_To_Left_With_A_Frozen_Filtered_Header`: `SplitRow` is 2 and the AutoFilter range is `"A2:G4"`.
- Every row/column index in the remaining tests moves to the new constants (data from row 3; session type in column 4).

Add:

```csharp
[TestMethod]
public void Writes_The_Full_Car_Name_As_A_Bold_Title_Across_The_Columns()
    // RequestDetailSheet.AddTo(workbook, "Corolla (2)", "Corolla", [Request()])
    // Worksheet("Corolla (2)"): cell (1, 1) text "Corolla", bold, merged range "A1:G1"
```

- [ ] **Step 2: Rewrite the generator tests for one sheet per car**

In `ExcelGeneratorTest.cs`, a helper `Request(Guid carId, string carName, string studentName = "Smoke Student A")` builds a `SlotRequestDetail` (Sunday, Morning, Single, target 1, picks 1, no constraint). Replace `Detail_Sheet_Lists_The_Requests_Of_The_Teacher_And_Publication` and `Workbook_Has_The_Summary_Then_The_Request_Detail_Sheet` with:

```csharp
[TestMethod]
public async Task Workbook_Is_The_Summary_Only_When_There_Are_No_Requests()
    // sheet names == [SummarySheet.Name]

[TestMethod]
public async Task Workbook_Has_The_Summary_Then_A_Sheet_Per_Car_By_Name()
    // requests for "Zeta" and "Alpha" (two car ids) -> [SummarySheet.Name, "Alpha", "Zeta"]

[TestMethod]
public async Task Each_Car_Sheet_Lists_Only_That_Cars_Requests()
    // "Alpha": student A; "Zeta": students B and C
    // Alpha: last used row 3, cell (3, 3) "Smoke Student A"; Zeta: last used row 4

[TestMethod]
public async Task Two_Cars_With_The_Same_Name_Get_A_Sheet_Each()
    // car ids Guid.Parse("00000000-0000-0000-0000-000000000001") and "...0002", both "Corolla", one request each
    // sheet names == [SummarySheet.Name, "Corolla", "Corolla (2)"]; each car sheet has one request row
    //   and title "Corolla"
```

- [ ] **Step 3: Run the tests to see them fail**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: FAIL. The build breaks on `RequestDetailSheet.AddTo`'s new signature.

- [ ] **Step 4: Change `RequestDetailSheet`**

- Remove `Name`, the `רכב` header and its width (Decision 8). Constants `TitleRow = 1`, `HeaderRow = 2`, `FirstRequestRow = 3`, `ConstraintsColumn = 7`.
- `AddTo(workbook, sheetName, carName, requests)`: add the sheet named `sheetName`, right-to-left; `FormatColumns`; `WriteTitle(sheet, carName)` (cell (1, 1) value `carName`, bold, `sheet.Range(TitleRow, FirstColumn, TitleRow, Headers.Length).Merge()`); `WriteHeader`; `WriteRequests`; AutoFilter from `HeaderRow` to `HeaderRow + requests.Count`; `FreezeRows(HeaderRow)`.
- `WriteRequests` orders by `Day`, `Window`, `StudentName` (Ordinal), `SubmissionId` (Decision 9); numbering per submission is unchanged. `WriteRequest` drops `CarName`.

- [ ] **Step 5: Change `ExcelGenerator`**

Replace `RequestDetailSheet.AddTo(workbook, requests)` with a private static `AddCarSheets(XLWorkbook workbook, IReadOnlyCollection<SlotRequestDetail> requests)`:
- group by `CarId`; order the groups by the group's first `CarName` (Ordinal), then by `CarId`; materialize to a list;
- `carNames` = each group's first `CarName`; `sheetNames = CarSheetNames.For(carNames)`;
- for each index, `RequestDetailSheet.AddTo(workbook, sheetNames[index], carNames[index], group.ToList())`.

No requests means no groups and the workbook is the summary only (Decision 3).

- [ ] **Step 6: Run the tests to see them pass**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: PASS, including `ExcelTypographyTest` and `SummarySheetTest`.

Then `dotnet build` and `dotnet test`. Expected: no new warnings, every test passes. `rg -n "RequestDetailSheet.Name" src tests` has no matches.

- [ ] **Step 7: The docs**

- `docs\requirements.md` §9: "One file per teacher per publication: a summary sheet, then one request-detail sheet per car." Rename "Sheet 2: Request Detail" to "Car sheets: Request Detail" and open it with: "One sheet per car that has requests, named after the car and ordered by car name. Row 1 is the car's full name; the rows are that car's slot requests, sorted by day, then slot, then student name." Drop the Car row. Add under the removed-columns notes: "The car column became a sheet per car at the customer's request (November 2026)."
- `docs\modules\excel\README.md`: add a slice-6 row (`6. Sheet per car ([plan](sheet-per-car-plan/README.md))` · "The detail sheet splits into one sheet per car, named after the car, with the car's name as a title row and no car column" · none (customer request, 1 November 2026)) and locked decision 9: "**One detail sheet per car that has requests, after the teacher-wide summary**, named after the car (made safe and unique), ordered by car name, with the car's full name in row 1 and no car column (customer request, 1 November 2026; slice 6)." Decision 7 ends with "Its car column became a sheet per car in decision 9."
- Fix any other current doc the `rg` in Files finds (e.g. a "two-sheet file" in the roadmap's slice-3 row stays as history).

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Infrastructure/Excel tests/DrivingLessons.Application.Test/Excel docs
git commit -m "feat(excel): one request-detail sheet per car, after the summary"
```

---

**Next:** [task-04-smoke-checks-and-push.md](task-04-smoke-checks-and-push.md)
