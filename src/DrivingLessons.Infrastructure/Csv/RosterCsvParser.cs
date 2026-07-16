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
    private const char NonBreakingSpace = ' ';

    private static readonly HashSet<char> BidiMarks =
    [
        '‎',
        '‏',
        '‪',
        '‫',
        '‬',
        '‭',
        '‮',
        '⁦',
        '⁧',
        '⁨',
        '⁩'
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
