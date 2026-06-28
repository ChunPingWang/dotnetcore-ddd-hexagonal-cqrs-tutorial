namespace BankAccountQuery.Domain.Common;

/// <summary>
/// 聚合根基底類別：負責收集領域內產生的 Domain Events。
/// 應用層在持久化成功後，取出事件交由 Dispatcher 發布，再清空。
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
