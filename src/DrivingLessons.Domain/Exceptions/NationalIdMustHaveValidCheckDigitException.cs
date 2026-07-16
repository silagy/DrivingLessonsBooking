using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class NationalIdMustHaveValidCheckDigitException : DomainException
{
    public NationalIdMustHaveValidCheckDigitException()
        : base("National ID must have a valid check digit.")
    {
    }
}
