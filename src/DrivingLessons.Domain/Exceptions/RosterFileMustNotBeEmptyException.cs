using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileMustNotBeEmptyException : DomainException
{
    public RosterFileMustNotBeEmptyException()
        : base("Roster file must contain at least one student row.")
    {
    }
}
