using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record RosterImportFailure
{
    public int RowNumber { get; }
    public string? StudentName { get; }
    public RosterRowFailureReason Reason { get; }

    private RosterImportFailure(int rowNumber, string? studentName, RosterRowFailureReason reason)
    {
        RowNumber = rowNumber;
        StudentName = studentName;
        Reason = reason;
    }

    public static RosterImportFailure Of(int rowNumber, string? studentName, RosterRowFailureReason reason)
    {
        if (rowNumber < 1)
        {
            throw new RosterImportFailureRowNumberMustBePositiveException(rowNumber);
        }

        return new RosterImportFailure(rowNumber, studentName, reason);
    }
}
