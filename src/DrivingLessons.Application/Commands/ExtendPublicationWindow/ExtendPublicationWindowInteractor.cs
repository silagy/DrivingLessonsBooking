using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ExtendPublicationWindow;

public class ExtendPublicationWindowInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationScheduler scheduler;
    private readonly IUnitOfWork unitOfWork;

    public ExtendPublicationWindowInteractor(
        IPublicationRepository repository,
        IPublicationScheduler scheduler,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.scheduler = scheduler;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, DateTimeOffset newEndUtc)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        publication.ExtendWindow(newEndUtc);

        await unitOfWork.CommitAsync();

        await scheduler.RescheduleCloseAsync(id, newEndUtc);
    }
}
