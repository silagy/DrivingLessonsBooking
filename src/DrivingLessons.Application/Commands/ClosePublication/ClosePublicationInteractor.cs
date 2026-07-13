using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ClosePublication;

public class ClosePublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationQueries publicationQueries;
    private readonly IUnitOfWork unitOfWork;

    public ClosePublicationInteractor(
        IPublicationRepository repository,
        IPublicationQueries publicationQueries,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.publicationQueries = publicationQueries;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var weekStart = publication.WeekStart.Value;
        var teacherGuids = await publicationQueries.GetTeacherIdsWithScheduleForWeekAsync(weekStart);
        var teacherIds = teacherGuids.Select(TeacherId.Of);

        publication.Close(teacherIds);

        await unitOfWork.CommitAsync();
    }
}
