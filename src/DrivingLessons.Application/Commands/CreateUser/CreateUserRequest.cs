using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateUser;

public record CreateUserRequest(string Name, string SignInEmail, Role Role, Guid? TeacherId, string TemporaryPassword);
