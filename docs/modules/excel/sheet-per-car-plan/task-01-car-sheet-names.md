# Task 1 of 4: Safe, unique sheet names for cars

> Part of [Sheet per Car](README.md). Read its Decisions and Global Constraints first. Branch `excel-sheet-per-car`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Infrastructure\Excel\CarSheetNames.cs`
- Test: `tests\DrivingLessons.Application.Test\Excel\CarSheetNamesTest.cs`

**Interfaces:**
- Consumes: `SummarySheet.Name` (`"סיכום"`).
- Produces: `public static class CarSheetNames { public static IReadOnlyList<string> For(IReadOnlyList<string> carNames); }`: one sheet name per car name, in the same order, following Decision 6.

- [ ] **Step 1: Write the failing tests**

`CarSheetNamesTest` (MSTest + Shouldly, `//given //when //then`):

```csharp
[TestMethod]
public void Keeps_A_Plain_Car_Name()
    // ["Corolla White"] -> ["Corolla White"]

[TestMethod]
[DataRow("A:B", "A-B")]
[DataRow("A\\B", "A-B")]
[DataRow("A/B", "A-B")]
[DataRow("A?B", "A-B")]
[DataRow("A*B", "A-B")]
[DataRow("[A]", "-A-")]
public void Replaces_Characters_Excel_Refuses_With_A_Hyphen(string carName, string expected)

[TestMethod]
[DataRow("'Mazda", "-Mazda")]
[DataRow("Mazda'", "Mazda-")]
[DataRow("Maz'da", "Maz'da")]
public void Replaces_A_Leading_Or_Trailing_Apostrophe(string carName, string expected)

[TestMethod]
public void Cuts_A_Long_Name_To_31_Characters()
    // [new string('א', 40)] -> [new string('א', 31)]

[TestMethod]
public void Numbers_Repeated_Names_Ignoring_Case()
    // ["Corolla", "Corolla", "corolla"] -> ["Corolla", "Corolla (2)", "corolla (3)"]

[TestMethod]
public void Keeps_A_Numbered_Long_Name_Within_31_Characters()
    // two of new string('א', 40) -> [31 × 'א', 27 × 'א' + " (2)"]

[TestMethod]
public void Skips_A_Number_Already_Taken()
    // ["Corolla (2)", "Corolla", "Corolla"] -> ["Corolla (2)", "Corolla", "Corolla (3)"]

[TestMethod]
[DataRow("סיכום")]
[DataRow("history")]
public void Never_Takes_A_Reserved_Name(string carName)
    // [carName] -> [$"{carName} (2)"]

[TestMethod]
public void Returns_No_Names_For_No_Cars()
    // [] -> []
```

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CarSheetNames"`
Expected: FAIL. The build breaks because `CarSheetNames` does not exist.

- [ ] **Step 3: Implement `CarSheetNames`**

`public static class CarSheetNames` in `DrivingLessons.Infrastructure.Excel`:
- Constants `MaxLength = 31`, `Replacement = '-'`, `Apostrophe = '\''`; `private static readonly char[] RefusedCharacters = [':', '\\', '/', '?', '*', '[', ']']`; `private static readonly string[] ReservedNames = [SummarySheet.Name, "History"]`.
- `For`: a `HashSet<string>` with `StringComparer.OrdinalIgnoreCase` seeded with `ReservedNames`; for each car name, `Safe(name)` then `Unique(safe, taken)`, add the result to `taken`, collect in order.
- `Safe`: replace refused characters, replace a leading and a trailing apostrophe, then `Cut(value, MaxLength)`.
- `Unique`: return the name if free; otherwise for `number = 2, 3, …` build `suffix = $" ({number})"` and `Cut(name, MaxLength - suffix.Length) + suffix` until free.
- No nested calls, no magic numbers, early returns (code-style.md).

- [ ] **Step 4: Run the tests to see them pass**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CarSheetNames"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Infrastructure/Excel/CarSheetNames.cs tests/DrivingLessons.Application.Test/Excel/CarSheetNamesTest.cs
git commit -m "feat(excel): safe, unique sheet names for cars"
```

---

**Next:** [task-02-car-id-in-read-model.md](task-02-car-id-in-read-model.md)
