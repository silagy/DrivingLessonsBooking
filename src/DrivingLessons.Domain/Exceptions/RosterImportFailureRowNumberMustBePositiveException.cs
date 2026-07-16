using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterImportFailureRowNumberMustBePositiveException : DomainException
{
    public RosterImportFailureRowNumberMustBePositiveException(int rowNumber)
        : base($"Roster import failure row number {rowNumber} must be positive.")
    {
    }
}
