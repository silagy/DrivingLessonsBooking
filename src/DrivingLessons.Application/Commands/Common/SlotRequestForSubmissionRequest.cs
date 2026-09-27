using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.Common;

public record SlotRequestForSubmissionRequest(Guid SlotId, SessionType SessionType, string? Constraint);
