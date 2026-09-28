using DrivingLessons.Application.Commands.Common;

namespace DrivingLessons.Application.Commands.ReviseSubmission;

public record ReviseSubmissionRequest(
    string NationalId,
    int TargetCount,
    IReadOnlyList<SlotRequestForSubmissionRequest> SlotRequests);
