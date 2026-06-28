namespace BankAccountQuery.Domain.Common;

/// <summary>
/// Domain Event 標記介面。領域內發生、具有業務意義的事實。
/// 不依賴任何框架（不是 MediatR 的 INotification）。
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
