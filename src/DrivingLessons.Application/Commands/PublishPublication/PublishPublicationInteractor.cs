using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.PublishPublication;

public class PublishPublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationScheduler scheduler;
    private readonly IUnitOfWork unitOfWork;

    public PublishPublicationInteractor(
        IPublicationRepository repository,
        IPublicationScheduler scheduler,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.scheduler = scheduler;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var window = SubmissionWindow.Of(startUtc, endUtc);
        publication.Publish(window);

        await unitOfWork.CommitAsync();

        await scheduler.SchedulePublicationJobsAsync(id, startUtc, endUtc);
    }
}
