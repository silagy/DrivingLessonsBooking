namespace DrivingLessons.Infrastructure.Excel;

public static class CarSheetNames
{
    private const int MaxLength = 31;
    private const int FirstRepeatNumber = 2;
    private const char Replacement = '-';
    private const char Apostrophe = '\'';

    private static readonly char[] RefusedCharacters =
    [
        ':',
        '\\',
        '/',
        '?',
        '*',
        '[',
        ']'
    ];

    private static readonly string[] ReservedNames =
    [
        SummarySheet.Name,
        "History"
    ];

    public static IReadOnlyList<string> For(IReadOnlyList<string> carNames)
    {
        var taken = new HashSet<string>(ReservedNames, StringComparer.OrdinalIgnoreCase);
        var sheetNames = new List<string>();

        foreach (var carName in carNames)
        {
            var safe = Safe(carName);
            var sheetName = Unique(safe, taken);

            taken.Add(sheetName);
            sheetNames.Add(sheetName);
        }

        return sheetNames;
    }

    private static string Safe(string carName)
    {
        var cut = Cut(carName, MaxLength);
        var characters = cut.ToCharArray();

        for (var index = 0; index < characters.Length; index++)
        {
            var isRefused = RefusedCharacters.Contains(characters[index]);
            var isEdge = index == 0 || index == characters.Length - 1;
            var isEdgeApostrophe = isEdge && characters[index] == Apostrophe;

            if (isRefused || isEdgeApostrophe)
            {
                characters[index] = Replacement;
            }
        }

        return new string(characters);
    }

    private static string Unique(string name, HashSet<string> taken)
    {
        if (!taken.Contains(name))
        {
            return name;
        }

        var number = FirstRepeatNumber;
        var candidate = Numbered(name, number);

        while (taken.Contains(candidate))
        {
            number++;
            candidate = Numbered(name, number);
        }

        return candidate;
    }

    private static string Numbered(string name, int number)
    {
        var suffix = $" ({number})";
        var baseName = Cut(name, MaxLength - suffix.Length);

        return baseName + suffix;
    }

    private static string Cut(string value, int length)
    {
        if (value.Length <= length)
        {
            return value;
        }

        return value[..length];
    }
}
