using DrivingLessons.Infrastructure.Outbox;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class ProcessOutboxJob : IJob
{
    public const string JobGroup = "outbox";

    private readonly OutboxProcessor processor;

    public ProcessOutboxJob(OutboxProcessor processor)
    {
        this.processor = processor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await processor.ProcessDueAsync(context.CancellationToken);
    }
}
