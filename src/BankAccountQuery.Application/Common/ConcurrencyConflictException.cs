namespace BankAccountQuery.Application.Common;

/// <summary>
/// 樂觀並行衝突：嘗試以過期的版本附加事件（其他寫入者已先寫入）。
/// 對應 HTTP 409。
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string streamId)
        : base($"事件串流 [{streamId}] 發生並行衝突，請重試") { }
}
