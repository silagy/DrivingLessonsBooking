using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarDetailsChanged(CarId CarId, CarName Name, CarType Type, Transmission Transmission) : IDomainEvent;
