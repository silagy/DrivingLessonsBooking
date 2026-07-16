using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RosterFileNameMustNotBeEmptyException : DomainException
{
    public RosterFileNameMustNotBeEmptyException()
        : base("Roster file name must not be empty.")
    {
    }
}
