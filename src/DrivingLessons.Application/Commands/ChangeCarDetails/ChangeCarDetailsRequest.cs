using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeCarDetails;

public record ChangeCarDetailsRequest(string Name, string Type, Transmission Transmission);
