using DrivingLessons.Application.Commands.Common;

namespace DrivingLessons.Application.Commands.CreateSubmission;

public record CreateSubmissionRequest(
    string NationalId,
    int TargetCount,
    IReadOnlyList<SlotRequestForSubmissionRequest> SlotRequests);
