using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeClosedException : DomainException
{
    public PublicationMustBeClosedException(PublicationId id)
        : base($"Publication {id.Value} must be closed.")
    {
    }
}
