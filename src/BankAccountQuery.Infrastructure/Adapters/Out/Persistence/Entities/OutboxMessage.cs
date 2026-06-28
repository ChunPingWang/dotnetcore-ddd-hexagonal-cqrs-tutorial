namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

/// <summary>
/// Outbox 訊息：與聚合狀態變更在「同一個交易」寫入，
/// 之後由背景處理器可靠地派發（至少一次）。
/// </summary>
public sealed class OutboxMessage
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;          // 領域事件型別（FullName）
    public string Content { get; set; } = default!;       // 序列化後的事件內容（JSON）
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }         // null = 尚未處理
    public string? Error { get; set; }
}
