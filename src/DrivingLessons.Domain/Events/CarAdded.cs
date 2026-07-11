using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarAdded(TeacherId TeacherId, CarId CarId, CarName Name, CarType Type, Transmission Transmission) : IDomainEvent;
