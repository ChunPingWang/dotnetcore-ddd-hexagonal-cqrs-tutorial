using BankAccountQuery.Domain.Common;

namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port：派發領域事件給對應的 IDomainEventHandler。
/// 由 Infrastructure 實作（從 DI 容器解析 handler），
/// 讓領域層不需認識 MediatR 或任何發布機制。
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyList<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
