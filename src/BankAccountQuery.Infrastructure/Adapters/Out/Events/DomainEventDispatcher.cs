using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Events;

/// <summary>
/// Driven Adapter：自 DI 容器解析每個事件型別對應的 IDomainEventHandler 並依序呼叫。
/// 讓領域層與應用層完全不需認識 DI 容器或任何發布機制。
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task DispatchAsync(
        IReadOnlyList<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in _serviceProvider.GetServices(handlerType))
            {
                if (handler is null) continue;
                await (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
            }
        }
    }
}
