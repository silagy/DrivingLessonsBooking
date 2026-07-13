using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeOpenException : DomainException
{
    public PublicationMustBeOpenException(PublicationId id)
        : base($"Publication {id.Value} must be open.")
    {
    }
}
