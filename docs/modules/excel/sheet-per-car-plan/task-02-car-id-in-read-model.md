# Task 2 of 4: Car id in the detail read model

> Part of [Sheet per Car](README.md). Requires task 1 committed. Branch `excel-sheet-per-car`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` (`SlotRequestDetail`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` (`GetSlotRequestDetailsAsync`)
- Modify: `tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs`, `ExcelGeneratorTest.cs`, `ExcelTypographyTest.cs` (constructor calls only)

**Interfaces:**
- Consumes: `Car.Id` (`CarId`, `.Value`), the existing car join (`IgnoreQueryFilters([QueryFilters.CarNotDeleted])`).
- Produces: `SlotRequestDetail` with the Decision 11 signature.

A pure shape change: nothing reads `CarId` yet, so every existing test must stay green.

- [ ] **Step 1: Change the record and every constructor call**

- `SlotRequestDetail` takes the Decision 11 signature (`Guid CarId` between `StudentName` and `CarName`).
- In `RequestDetailSheetTest.Request(...)` add a parameter `Guid? carId = null` before `carName` and pass `carId ?? Guid.NewGuid()`.
- In `ExcelGeneratorTest` and `ExcelTypographyTest` pass `Guid.NewGuid()` before the car name.

- [ ] **Step 2: Project the car id**

In `GetSlotRequestDetailsAsync`, keep every join and add `CarId = car.Id` beside `CarName = car.Name`; map `x.CarId.Value` before `x.CarName.Value`.

- [ ] **Step 3: Build and run the Excel tests**

Run: `dotnet build` then `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: no new warnings; PASS.

- [ ] **Step 4: Commit**

```bash
git add src/DrivingLessons.Application/Queries/ISubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/EntityFramework/Queries/SubmissionQueries.cs \
  tests/DrivingLessons.Application.Test/Excel
git commit -m "feat(excel): detail read model carries the student's car id"
```

---

**Next:** [task-03-one-sheet-per-car.md](task-03-one-sheet-per-car.md)
