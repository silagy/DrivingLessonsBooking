using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record CarAssignedToTeacher(CarId CarId, TeacherId TeacherId) : IDomainEvent;
