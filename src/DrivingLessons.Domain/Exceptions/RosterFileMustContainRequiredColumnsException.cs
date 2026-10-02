using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileMustContainRequiredColumnsException : DomainException
{
    public IReadOnlyCollection<string> MissingColumns { get; }

    public RosterFileMustContainRequiredColumnsException(IReadOnlyCollection<string> missingColumns)
        : base(BuildMessage(missingColumns))
    {
        MissingColumns = missingColumns;
    }

    private static string BuildMessage(IReadOnlyCollection<string> missingColumns)
    {
        var columns = string.Join(", ", missingColumns);

        return $"Roster file must contain required columns: {columns}.";
    }
}
