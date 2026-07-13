using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarDeleted(CarId CarId) : IDomainEvent;
