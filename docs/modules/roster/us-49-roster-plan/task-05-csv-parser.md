# Task 5 of 10: Roster CSV parser (port + adapter)

> Part of [US-49: Roster Module](README.md). Requires tasks 1–4 complete. Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Application\Abstractions\IRosterCsvParser.cs`, `RosterCsvRow.cs`
- Create: `src\DrivingLessons.Infrastructure\Csv\RosterCsvParser.cs`, `RosterCsvHeaders.cs`
- Create: `tests\DrivingLessons.Application.Test\Csv\RosterCsvParserTest.cs`

The port lives in `Application\Abstractions\` next to `IExcelGenerator.cs` — open it first and mirror its shape. The adapter lives in a new `Infrastructure\Csv\` folder, mirroring `Infrastructure\Excel\` (see `PlaceholderExcelGenerator.cs` for namespace style). Tests go in the existing `DrivingLessons.Application.Test` project (MSTest + Shouldly, already referenced).

**Decision: hand-rolled RFC-4180, no new NuGet package.** No CSV library exists in any csproj today; the Berosh export is ~60 rows; a fully-tested ~60-line state machine is smaller than a dependency. If CsvHelper is ever wanted, the swap stays behind `IRosterCsvParser`.

- [ ] **Step 1: The port**

`src\DrivingLessons.Application\Abstractions\IRosterCsvParser.cs`:

```csharp
namespace DrivingLessons.Application.Abstractions;

public interface IRosterCsvParser
{
    IReadOnlyList<RosterCsvRow> Parse(Stream content);
}
```

`src\DrivingLessons.Application\Abstractions\RosterCsvRow.cs` — raw text only, all cells nullable; interpretation (checksum, date parsing, lookups) happens in the interactor in Task 6:

```csharp
namespace DrivingLessons.Application.Abstractions;

public class RosterCsvRow
{
    public int RowNumber { get; init; }
    public string? FullName { get; init; }
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? TeacherName { get; init; }
    public string? CarName { get; init; }
    public string? Address { get; init; }
    public string? StartDate { get; init; }
    public string? LicenseType { get; init; }
}
```

- [ ] **Step 2: The header map — the single swap file**

If Berosh ever renames a column, this is the only file that changes. The notes column `הערות` (ignored per decision 13) and any other Berosh bookkeeping columns are deliberately absent — unknown columns are simply skipped.

`src\DrivingLessons.Infrastructure\Csv\RosterCsvHeaders.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Csv;

public static class RosterCsvHeaders
{
    public const string FullName = "שם מלא";
    public const string NationalId = "תעודת זהות";
    public const string Phone = "טלפון";
    public const string Teacher = "מורה";
    public const string Car = "רכב";
    public const string Address = "כתובת";
    public const string StartDate = "תאריך התחלה";
    public const string LicenseType = "סוג רישיון";

    public static readonly string[] Required =
    [
        FullName,
        NationalId,
        Phone,
        Teacher,
        Car
    ];
}
```

- [ ] **Step 3: The parser**

Behavior contract:
- `StreamReader` with `detectEncodingFromByteOrderMarks: true` consumes the UTF-8 BOM Excel writes
- Char state machine: quoted fields, `""` escape, embedded commas and CR/LF inside quotes, CRLF and LF line endings
- Per-field cleanup: strip bidi control marks (U+200E/U+200F, U+202A-U+202E, U+2066-U+2069), NBSP (U+00A0) to regular space, then trim; a field that cleans to empty becomes a `null` property
- Header row matched **by name**, order-independent; unknown columns ignored
- Missing required header → `RosterFileMustContainRequiredColumnsException`; empty or header-only file → `RosterFileMustNotBeEmptyException` (both created in Task 4 as `DomainException`s)
- `RowNumber` counts records from the header: header is row 1, first data row is 2 — matching what the admin sees in Excel

> The two exceptions' existing constructor signatures win — check them before use. This plan assumes `RosterFileMustContainRequiredColumnsException(IReadOnlyCollection<string> missingColumns)` and a parameterless `RosterFileMustNotBeEmptyException`.

`src\DrivingLessons.Infrastructure\Csv\RosterCsvParser.cs`:

```csharp
using System.Text;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Infrastructure.Csv;

public class RosterCsvParser : IRosterCsvParser
{
    private sealed record CsvRecord(int RowNumber, IReadOnlyList<string> Fields);

    private const char Quote = '"';
    private const char Comma = ',';
    private const char CarriageReturn = '\r';
    private const char LineFeed = '\n';
    private const char NonBreakingSpace = '\u00A0';

    private static readonly HashSet<char> BidiMarks =
    [
        '\u200E',
        '\u200F',
        '\u202A',
        '\u202B',
        '\u202C',
        '\u202D',
        '\u202E',
        '\u2066',
        '\u2067',
        '\u2068',
        '\u2069'
    ];

    public IReadOnlyList<RosterCsvRow> Parse(Stream content)
    {
        var records = ReadRecords(content);

        if (!records.Any())
        {
            throw new RosterFileMustNotBeEmptyException();
        }

        var header = records[0];
        var columns = MapColumns(header.Fields);

        if (records.Count == 1)
        {
            throw new RosterFileMustNotBeEmptyException();
        }

        return records
                   .Skip(1)
                   .Select(record => ToRow(record, columns))
                   .ToList();
    }

    private static List<CsvRecord> ReadRecords(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var records = new List<CsvRecord>();
        var fields = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;
        var recordStarted = false;
        var rowNumber = 1;

        while (reader.Peek() != -1)
        {
            var character = (char)reader.Read();

            if (inQuotes)
            {
                if (character != Quote)
                {
                    currentField.Append(character);
                    continue;
                }

                if (reader.Peek() == Quote)
                {
                    reader.Read();
                    currentField.Append(Quote);
                    continue;
                }

                inQuotes = false;
                continue;
            }

            switch (character)
            {
                case Quote:
                    inQuotes = true;
                    recordStarted = true;
                    break;
                case Comma:
                    fields.Add(CleanField(currentField));
                    recordStarted = true;
                    break;
                case CarriageReturn:
                    break;
                case LineFeed:
                    if (recordStarted)
                    {
                        fields.Add(CleanField(currentField));
                        records.Add(new CsvRecord(rowNumber, fields));
                        fields = [];
                        rowNumber++;
                    }

                    recordStarted = false;
                    break;
                default:
                    currentField.Append(character);
                    recordStarted = true;
                    break;
            }
        }

        if (recordStarted)
        {
            fields.Add(CleanField(currentField));
            records.Add(new CsvRecord(rowNumber, fields));
        }

        return records;
    }

    private static Dictionary<string, int> MapColumns(IReadOnlyList<string> headerFields)
    {
        var columns = new Dictionary<string, int>();

        for (var index = 0; index < headerFields.Count; index++)
        {
            columns.TryAdd(headerFields[index], index);
        }

        var missing = RosterCsvHeaders.Required
                                      .Where(required => !columns.ContainsKey(required))
                                      .ToList();

        if (missing.Any())
        {
            throw new RosterFileMustContainRequiredColumnsException(missing);
        }

        return columns;
    }

    private static RosterCsvRow ToRow(CsvRecord record, IReadOnlyDictionary<string, int> columns)
    {
        return new RosterCsvRow
        {
            RowNumber = record.RowNumber,
            FullName = FieldOrNull(record, columns, RosterCsvHeaders.FullName),
            NationalId = FieldOrNull(record, columns, RosterCsvHeaders.NationalId),
            Phone = FieldOrNull(record, columns, RosterCsvHeaders.Phone),
            TeacherName = FieldOrNull(record, columns, RosterCsvHeaders.Teacher),
            CarName = FieldOrNull(record, columns, RosterCsvHeaders.Car),
            Address = FieldOrNull(record, columns, RosterCsvHeaders.Address),
            StartDate = FieldOrNull(record, columns, RosterCsvHeaders.StartDate),
            LicenseType = FieldOrNull(record, columns, RosterCsvHeaders.LicenseType)
        };
    }

    private static string? FieldOrNull(
        CsvRecord record,
        IReadOnlyDictionary<string, int> columns,
        string header)
    {
        if (!columns.TryGetValue(header, out var index))
        {
            return null;
        }

        if (index >= record.Fields.Count)
        {
            return null;
        }

        var value = record.Fields[index];

        return value.Length == 0 ? null : value;
    }

    private static string CleanField(StringBuilder currentField)
    {
        var raw = currentField.ToString();
        currentField.Clear();

        var cleaned = new StringBuilder(raw.Length);

        foreach (var character in raw)
        {
            if (BidiMarks.Contains(character))
            {
                continue;
            }

            if (character == NonBreakingSpace)
            {
                cleaned.Append(' ');
                continue;
            }

            cleaned.Append(character);
        }

        return cleaned.ToString().Trim();
    }
}
```

- [ ] **Step 4: Tests**

Build streams in-code with the Hebrew headers; the BOM test writes `Encoding.UTF8.GetPreamble()` bytes explicitly.

`tests\DrivingLessons.Application.Test\Csv\RosterCsvParserTest.cs`:

```csharp
using System.Text;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Infrastructure.Csv;
using Shouldly;

namespace DrivingLessons.Application.Test.Csv;

[TestClass]
public class RosterCsvParserTest
{
    private const string Header = "שם מלא,תעודת זהות,טלפון,מורה,רכב,כתובת,תאריך התחלה,סוג רישיון";

    [TestMethod]
    public void Parses_Rows_By_Header_Name()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,הרצל 5,01/09/2025,B";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.NationalId.ShouldBe("123456782");
        row.Phone.ShouldBe("0501234567");
        row.TeacherName.ShouldBe("משה לוי");
        row.CarName.ShouldBe("טויוטה 123");
        row.Address.ShouldBe("הרצל 5");
        row.StartDate.ShouldBe("01/09/2025");
        row.LicenseType.ShouldBe("B");
    }

    [TestMethod]
    public void Header_Order_Is_Irrelevant()
    {
        //given
        var csv = "טלפון,רכב,מורה,שם מלא,תעודת זהות\n0501234567,טויוטה 123,משה לוי,דנה כהן,123456782";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.NationalId.ShouldBe("123456782");
        row.Phone.ShouldBe("0501234567");
        row.TeacherName.ShouldBe("משה לוי");
        row.CarName.ShouldBe("טויוטה 123");
        row.Address.ShouldBeNull();
    }

    [TestMethod]
    public void Unknown_Columns_Are_Ignored()
    {
        //given
        var csv = Header + ",הערות\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,,,B,מתקדמת מהר";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.LicenseType.ShouldBe("B");
    }

    [TestMethod]
    public void Utf8_Bom_Is_Consumed()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,,,";
        var preamble = Encoding.UTF8.GetPreamble();
        var csvBytes = Encoding.UTF8.GetBytes(csv);
        using var stream = new MemoryStream();
        stream.Write(preamble);
        stream.Write(csvBytes);
        stream.Position = 0;
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
    }

    [TestMethod]
    public void Quoted_Field_Keeps_Embedded_Comma()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,\"הרצל 5, תל אביב\",,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.Address.ShouldBe("הרצל 5, תל אביב");
        row.StartDate.ShouldBeNull();
    }

    [TestMethod]
    public void Escaped_Quotes_Are_Unescaped()
    {
        //given
        var csv = Header + "\n\"דנה \"\"דני\"\" כהן\",123456782,0501234567,משה לוי,טויוטה 123,,,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה \"דני\" כהן");
    }

    [TestMethod]
    public void Quoted_Field_Keeps_Embedded_Newline()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,\"הרצל 5\nתל אביב\",,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.Address.ShouldBe("הרצל 5\nתל אביב");
    }

    [TestMethod]
    public void Directional_Marks_Are_Stripped()
    {
        //given
        var csv = Header + "\n\u202Bדנה כהן\u202C,\u200F123456782\u200E,050\u00A01234567,משה לוי,טויוטה 123,,,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.NationalId.ShouldBe("123456782");
        row.Phone.ShouldBe("050 1234567");
    }

    [TestMethod]
    public void Missing_Required_Header_Throws()
    {
        //given
        var csv = "שם מלא,תעודת זהות,מורה,רכב\nדנה כהן,123456782,משה לוי,טויוטה 123";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var act = () => parser.Parse(stream);

        //then
        Should.Throw<RosterFileMustContainRequiredColumnsException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(Header)]
    public void Empty_File_Throws(string csv)
    {
        //given
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var act = () => parser.Parse(stream);

        //then
        Should.Throw<RosterFileMustNotBeEmptyException>(act);
    }

    [TestMethod]
    public void Row_Numbers_Count_From_The_Header()
    {
        //given
        var csv = Header
                  + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,,,"
                  + "\r\nיוסי מזרחי,987654324,0521234567,משה לוי,טויוטה 123,,,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        rows[0].RowNumber.ShouldBe(2);
        rows[1].RowNumber.ShouldBe(3);
    }

    private static MemoryStream StreamOf(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        return new MemoryStream(bytes);
    }
}
```

- [ ] **Step 5: Build + run all tests**

Run: `dotnet build`
Expected: success.

Run:
```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```
Expected: all tests PASS, including the 11 new parser tests.

- [ ] **Step 6: Commit**

```bash
git add src/DrivingLessons.Application src/DrivingLessons.Infrastructure tests/DrivingLessons.Application.Test
git commit -m "feat(infrastructure): add header-mapped RFC-4180 roster CSV parser"
```

---

**Next:** [task-06-application-layer.md](task-06-application-layer.md)
