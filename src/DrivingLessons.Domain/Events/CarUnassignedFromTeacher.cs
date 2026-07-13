using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarUnassignedFromTeacher(CarId CarId, TeacherId TeacherId) : IDomainEvent;
