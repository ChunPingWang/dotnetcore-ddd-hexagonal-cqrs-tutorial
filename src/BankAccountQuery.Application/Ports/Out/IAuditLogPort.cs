namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port：稽核日誌寫入。由 Infrastructure 的 Adapter 實作。
/// </summary>
public interface IAuditLogPort
{
    Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// 稽核日誌條目。
/// </summary>
public sealed record AuditLogEntry(
    string CustomerId,
    string QueryType,
    string? IpAddress,
    DateTime Timestamp);
