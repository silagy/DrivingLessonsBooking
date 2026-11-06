using DrivingLessons.Infrastructure.Outbox;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class PurgeOutboxJob : IJob
{
    private readonly OutboxProcessor processor;

    public PurgeOutboxJob(OutboxProcessor processor)
    {
        this.processor = processor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await processor.PurgeAsync();
    }
}
