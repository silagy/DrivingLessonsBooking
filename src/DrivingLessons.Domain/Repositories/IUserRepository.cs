using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email signInEmail);

    Task<bool> AnyExistAsync();

    void Add(User user);
}
