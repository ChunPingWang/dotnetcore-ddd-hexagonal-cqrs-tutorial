using System.Collections.Concurrent;
using BankAccountQuery.Application.Ports.Out;
using Microsoft.Extensions.Logging;

namespace BankAccountQuery.Infrastructure.Adapters.Out.AuditLog;

/// <summary>
/// Driven Adapter：稽核日誌寫入（示範以記憶體 + Logger 實作，
/// 正式環境可替換為 PostgresAuditLogAdapter）。
/// </summary>
public sealed class InMemoryAuditLogAdapter : IAuditLogPort
{
    private static readonly ConcurrentQueue<AuditLogEntry> Entries = new();
    private readonly ILogger<InMemoryAuditLogAdapter> _logger;

    public InMemoryAuditLogAdapter(ILogger<InMemoryAuditLogAdapter> logger)
        => _logger = logger;

    public Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Enqueue(entry);
        _logger.LogInformation(
            "稽核日誌：客戶 {CustomerId} 執行 {QueryType}（IP={IpAddress}）於 {Timestamp:O}",
            entry.CustomerId, entry.QueryType, entry.IpAddress ?? "unknown", entry.Timestamp);
        return Task.CompletedTask;
    }

    public static IReadOnlyCollection<AuditLogEntry> Snapshot() => Entries.ToArray();
}
