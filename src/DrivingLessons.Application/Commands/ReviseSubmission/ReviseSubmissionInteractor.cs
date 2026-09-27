using DrivingLessons.Application.Commands.Common;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReviseSubmission;

public class ReviseSubmissionInteractor
{
    private readonly SubmissionContextResolver contextResolver;
    private readonly ISubmissionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public ReviseSubmissionInteractor(
        SubmissionContextResolver contextResolver,
        ISubmissionRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.contextResolver = contextResolver;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(string linkToken, ReviseSubmissionRequest request)
    {
        var context = await contextResolver.ResolveAsync(linkToken, request.NationalId);
        var publication = context.Publication;
        var student = context.Student;
        var weekSchedule = context.WeekSchedule;

        var submission = await repository.GetByPublicationAndStudentAsync(publication.Id, student.Id)
                         ?? throw new SubmissionNotFoundException();

        var targetCount = TargetSessionCount.Of(request.TargetCount);
        var picks = SubmissionContextResolver.ResolvePicks(weekSchedule, request.SlotRequests);
        var revisedAtUtc = timeProvider.GetUtcNow();

        submission.Revise(publication, student, weekSchedule, targetCount, picks, revisedAtUtc);

        await unitOfWork.CommitAsync();
    }
}
