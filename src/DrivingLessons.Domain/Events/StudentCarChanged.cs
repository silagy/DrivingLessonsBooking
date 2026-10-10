using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentCarChanged(StudentId StudentId, CarId CarId) : IDomainEvent;
