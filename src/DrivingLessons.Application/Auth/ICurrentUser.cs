using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public interface ICurrentUser
{
    UserId Id { get; }
}
