using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class PhoneNumberMustBeValidException : DomainException
{
    public PhoneNumberMustBeValidException()
        : base("Phone number must be a valid phone number.")
    {
    }
}
