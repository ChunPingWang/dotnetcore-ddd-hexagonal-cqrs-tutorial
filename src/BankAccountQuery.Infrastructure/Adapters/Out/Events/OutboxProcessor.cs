using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Events;

/// <summary>
/// Outbox 處理器（可測試的核心邏輯）：讀取尚未處理的 Outbox 訊息，
/// 反序列化為領域事件並透過 Dispatcher 派發，成功後標記 ProcessedOnUtc；
/// 單筆失敗只記錄 Error 並繼續，不影響其他訊息。
/// </summary>
public sealed class OutboxProcessor
{
    private readonly BankDbContext _dbContext;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        BankDbContext dbContext,
        IDomainEventDispatcher dispatcher,
        ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>處理一批尚未派發的訊息，回傳成功派發的筆數。</summary>
    public async Task<int> ProcessPendingAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return 0;

        var processed = 0;
        foreach (var message in pending)
        {
            try
            {
                var domainEvent = DomainEventSerialization.Deserialize(message.Type, message.Content);
                await _dispatcher.DispatchAsync(new[] { domainEvent }, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
                processed++;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Outbox 訊息 {Id}（{Type}）派發失敗", message.Id, message.Type);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return processed;
    }
}
