using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileMustContainRequiredColumnsException : DomainException
{
    public RosterFileMustContainRequiredColumnsException(IReadOnlyCollection<string> missingColumns)
        : base(BuildMessage(missingColumns))
    {
    }

    private static string BuildMessage(IReadOnlyCollection<string> missingColumns)
    {
        var columns = string.Join(", ", missingColumns);

        return $"Roster file must contain required columns: {columns}.";
    }
}
