using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileMustBeCsvException : DomainException
{
    public RosterFileMustBeCsvException()
        : base("Roster file must be a CSV file, not an Excel workbook.")
    {
    }
}
