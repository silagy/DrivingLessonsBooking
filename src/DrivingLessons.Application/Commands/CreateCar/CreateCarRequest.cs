using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateCar;

public record CreateCarRequest(string Name, string Type, Transmission Transmission);
