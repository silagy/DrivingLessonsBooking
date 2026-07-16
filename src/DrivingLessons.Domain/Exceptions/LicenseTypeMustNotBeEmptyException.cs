using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class LicenseTypeMustNotBeEmptyException : DomainException
{
    public LicenseTypeMustNotBeEmptyException()
        : base("License type must not be empty.")
    {
    }
}
