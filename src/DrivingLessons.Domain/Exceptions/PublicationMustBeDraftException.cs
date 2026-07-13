using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeDraftException : DomainException
{
    public PublicationMustBeDraftException(PublicationId id)
        : base($"Publication {id.Value} must be draft.")
    {
    }
}
