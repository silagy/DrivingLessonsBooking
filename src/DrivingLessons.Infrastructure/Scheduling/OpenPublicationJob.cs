using DrivingLessons.Application.Commands.OpenPublication;
using DrivingLessons.Domain.Common;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class OpenPublicationJob : IJob
{
    public const string PublicationIdKey = "publicationId";

    private readonly OpenPublicationInteractor interactor;

    public OpenPublicationJob(OpenPublicationInteractor interactor)
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
