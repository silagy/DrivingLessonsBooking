using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class NationalIdMustBeAtMostNineDigitsException : DomainException
{
    public NationalIdMustBeAtMostNineDigitsException()
        : base("National ID must be at most nine digits.")
    {
    }
}
