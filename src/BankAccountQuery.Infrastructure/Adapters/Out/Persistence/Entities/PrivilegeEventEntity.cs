namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

/// <summary>
/// 事件溯源的事件儲存列（append-only）。
/// (StreamId, Version) 為複合主鍵，天然提供樂觀並行控制。
/// </summary>
public sealed class PrivilegeEventEntity
{
    public string StreamId { get; set; } = default!;   // = PrivilegeId
    public long Version { get; set; }                  // 串流內遞增（1-based）
    public string Type { get; set; } = default!;       // 領域事件型別（FullName）
    public string Payload { get; set; } = default!;    // 序列化後的事件（JSON）
    public DateTime OccurredOnUtc { get; set; }
}
