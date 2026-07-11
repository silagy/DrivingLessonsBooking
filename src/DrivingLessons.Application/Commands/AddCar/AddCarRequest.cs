using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.AddCar;

public record AddCarRequest(string Name, string Type, Transmission Transmission);
