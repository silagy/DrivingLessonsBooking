using DrivingLessons.Application.Commands.ClosePublication;
using DrivingLessons.Domain.Common;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class ClosePublicationJob : IJob
{
    public const string PublicationIdKey = "publicationId";

    private readonly ClosePublicationInteractor interactor;

    public ClosePublicationJob(ClosePublicationInteractor interactor)
    {
        this.interactor = interactor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var raw = context.MergedJobDataMap.GetString(PublicationIdKey);
        var publicationId = Guid.Parse(raw!);

        try
        {
            await interactor.ExecuteAsync(publicationId);
        }
        catch (DomainException)
        {
        }
    }
}
