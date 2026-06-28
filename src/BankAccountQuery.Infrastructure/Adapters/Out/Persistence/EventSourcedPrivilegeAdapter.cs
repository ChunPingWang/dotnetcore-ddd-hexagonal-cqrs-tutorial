using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.Out.Events;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// 事件溯源版的 Driven Adapter：以事件串流重建聚合（讀），並以 append-only
/// 附加新事件（寫，含樂觀並行）。同時實作 ILoadPrivilegePort 與 ISavePrivilegePort，
/// 可透過設定與狀態儲存版（PrivilegeEfCoreAdapter）互換。
/// </summary>
public sealed class EventSourcedPrivilegeAdapter : ILoadPrivilegePort, ISavePrivilegePort
{
    private readonly BankDbContext _dbContext;

    public EventSourcedPrivilegeAdapter(BankDbContext dbContext) => _dbContext = dbContext;

    public async Task<TransferPrivilege?> FindByPrivilegeIdAsync(
        PrivilegeId privilegeId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.PrivilegeEvents
            .AsNoTracking()
            .Where(e => e.StreamId == privilegeId.Value)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return null;

        return TransferPrivilege.Load(
            rows.Select(r => DomainEventSerialization.Deserialize(r.Type, r.Payload)));
    }

    public async Task<IReadOnlyList<TransferPrivilege>> FindByCustomerIdAsync(
        CustomerId customerId, CancellationToken cancellationToken = default)
    {
        // 掃描創生事件（GrantedEvent）找出該客戶擁有的 stream。
        // 註：正式系統應建立「客戶 → stream」投影/索引，避免全表掃描。
        var grantedType = typeof(TransferPrivilegeGrantedEvent).FullName;
        var grantedRows = await _dbContext.PrivilegeEvents
            .AsNoTracking()
            .Where(e => e.Type == grantedType)
            .ToListAsync(cancellationToken);

        var streamIds = grantedRows
            .Select(r => (r.StreamId,
                Event: (TransferPrivilegeGrantedEvent)DomainEventSerialization.Deserialize(r.Type, r.Payload)))
            .Where(x => x.Event.OwnerId == customerId)
            .Select(x => x.StreamId)
            .Distinct()
            .ToList();

        var result = new List<TransferPrivilege>();
        foreach (var streamId in streamIds)
        {
            var aggregate = await FindByPrivilegeIdAsync(PrivilegeId.Of(streamId), cancellationToken);
            if (aggregate is not null) result.Add(aggregate);
        }
        return result;
    }

    public async Task SaveAsync(TransferPrivilege privilege, CancellationToken cancellationToken = default)
    {
        var newEvents = privilege.DomainEvents;
        if (newEvents.Count == 0) return;

        // 載入時的版本 = 目前版本 − 本次新增的事件數（用於樂觀並行）
        var baseVersion = privilege.Version - newEvents.Count;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var domainEvent = newEvents[i];
            var (type, payload) = DomainEventSerialization.Serialize(domainEvent);

            _dbContext.PrivilegeEvents.Add(new PrivilegeEventEntity
            {
                StreamId = privilege.PrivilegeId.Value,
                Version = baseVersion + i + 1,
                Type = type,
                Payload = payload,
                OccurredOnUtc = domainEvent.OccurredOn
            });

            // 同步寫入 Outbox（同一交易）以便對外可靠派發
            _dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = type,
                Content = payload,
                OccurredOnUtc = domainEvent.OccurredOn,
                ProcessedOnUtc = null
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            // (StreamId, Version) 主鍵衝突 = 其他寫入者已搶先附加同一版本
            throw new ConcurrencyConflictException(privilege.PrivilegeId.Value);
        }
    }

    private static bool IsConcurrencyConflict(Exception ex) =>
        ex is DbUpdateException
        || (ex is ArgumentException && ex.Message.Contains("same key", StringComparison.OrdinalIgnoreCase));
}
