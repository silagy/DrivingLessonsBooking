using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBePublishedException : DomainException
{
    public PublicationMustBePublishedException(PublicationId id)
        : base($"Publication {id.Value} must be published.")
    {
    }
}
