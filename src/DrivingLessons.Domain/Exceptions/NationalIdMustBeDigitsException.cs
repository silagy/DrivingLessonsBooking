using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class NationalIdMustBeDigitsException : DomainException
{
    public NationalIdMustBeDigitsException()
        : base("National ID must contain only digits.")
    {
    }
}
