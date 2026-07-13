using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Infrastructure.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private const string HandleMethodName = nameof(IDomainEventHandler<IDomainEvent>.HandleAsync);

    private readonly IServiceProvider serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handleMethod = handlerType.GetMethod(HandleMethodName);
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var result = handleMethod!.Invoke(handler, [domainEvent]);
            var task = (Task)result!;

            await task;
        }
    }
}
