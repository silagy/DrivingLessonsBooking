using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class ShareableLinkTokenMustNotBeEmptyException : DomainException
{
    public ShareableLinkTokenMustNotBeEmptyException()
        : base("Shareable link token must not be empty.")
    {
    }
}
