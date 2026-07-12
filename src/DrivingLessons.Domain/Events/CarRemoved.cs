using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarRemoved(TeacherId TeacherId, CarId CarId) : IDomainEvent;
