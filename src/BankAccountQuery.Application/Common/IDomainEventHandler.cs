using BankAccountQuery.Domain.Common;

namespace BankAccountQuery.Application.Common;

/// <summary>
/// 領域事件處理者。一個事件可有多個處理者（皆會被派發）。
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
