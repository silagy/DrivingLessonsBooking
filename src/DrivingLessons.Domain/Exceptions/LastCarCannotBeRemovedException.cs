using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class LastCarCannotBeRemovedException : DomainException
{
    public LastCarCannotBeRemovedException(TeacherId id)
        : base($"The last car of teacher {id.Value} cannot be removed.")
    {
    }
}
