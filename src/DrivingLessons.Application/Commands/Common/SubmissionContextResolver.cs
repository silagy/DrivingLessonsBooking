using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.Common;

public class SubmissionContextResolver
{
    private readonly IPublicationRepository publicationRepository;
    private readonly IStudentRepository studentRepository;
    private readonly IWeekScheduleRepository weekScheduleRepository;

    public SubmissionContextResolver(
        IPublicationRepository publicationRepository,
        IStudentRepository studentRepository,
        IWeekScheduleRepository weekScheduleRepository)
    {
        this.publicationRepository = publicationRepository;
        this.studentRepository = studentRepository;
        this.weekScheduleRepository = weekScheduleRepository;
    }

    public async Task<SubmissionContext> ResolveAsync(string linkToken, string nationalId)
    {
        var token = ShareableLinkToken.Of(linkToken);

        var publication = await publicationRepository.GetByLinkTokenAsync(token);

        if (publication is null || publication.IsDraft)
        {
            throw new PublicationLinkNotFoundException();
        }

        var resolvedNationalId = NationalId.Of(nationalId);

        var student = await studentRepository.GetActiveByNationalIdAsync(resolvedNationalId)
                      ?? throw new StudentNotFoundException();

        var weekStart = publication.WeekStart;

        var weekSchedule = await weekScheduleRepository.GetByTeacherAndWeekAsync(student.TeacherId, weekStart)
                           ?? throw new WeekScheduleNotFoundException();

        return new SubmissionContext(publication, student, weekSchedule);
    }

    public static IReadOnlyList<SlotPick> ResolvePicks(
        WeekSchedule weekSchedule,
        IReadOnlyList<SlotRequestForSubmissionRequest> slotRequests)
    {
        var picks = new List<SlotPick>();

        foreach (var slotRequest in slotRequests)
        {
            var slotId = SlotId.Of(slotRequest.SlotId);

            var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == slotId)
                       ?? throw new SlotNotFoundException();

            var constraint = slotRequest.Constraint is null
                ? null
                : SlotConstraint.Of(slotRequest.Constraint);

            var pick = SlotPick.Of(slot, slotRequest.SessionType, constraint);
            picks.Add(pick);
        }

        return picks;
    }
}
