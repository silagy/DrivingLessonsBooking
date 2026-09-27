using DrivingLessons.Application.Commands.Common;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateSubmission;

public class CreateSubmissionInteractor
{
    private readonly SubmissionContextResolver contextResolver;
    private readonly ISubmissionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public CreateSubmissionInteractor(
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

    public async Task ExecuteAsync(string linkToken, CreateSubmissionRequest request)
    {
        var context = await contextResolver.ResolveAsync(linkToken, request.NationalId);
        var publication = context.Publication;
        var student = context.Student;
        var weekSchedule = context.WeekSchedule;

        var existingSubmission = await repository.GetByPublicationAndStudentAsync(publication.Id, student.Id);

        if (existingSubmission is not null)
        {
            throw new SubmissionAlreadyExistsException();
        }

        var targetCount = TargetSessionCount.Of(request.TargetCount);
        var picks = SubmissionContextResolver.ResolvePicks(weekSchedule, request.SlotRequests);
        var submittedAtUtc = timeProvider.GetUtcNow();

        var submission = Submission.Create(publication, student, weekSchedule, targetCount, picks, submittedAtUtc);

        repository.Add(submission);

        await unitOfWork.CommitAsync();
    }
}
