using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class PublicationNotFoundException : NotFoundException
{
    public PublicationNotFoundException(PublicationId id)
        : base($"Publication {id.Value} was not found.")
    {
    }

    public PublicationNotFoundException(DateOnly weekStart)
        : base($"Publication for week {weekStart:yyyy-MM-dd} was not found.")
    {
    }
}
