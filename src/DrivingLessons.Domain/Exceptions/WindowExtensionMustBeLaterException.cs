using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class WindowExtensionMustBeLaterException : DomainException
{
    public WindowExtensionMustBeLaterException(PublicationId id, DateTimeOffset newEndUtc)
        : base($"Publication {id.Value} window extension {newEndUtc:O} must be later than the current end.")
    {
    }
}
