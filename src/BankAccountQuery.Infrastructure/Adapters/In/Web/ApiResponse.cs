namespace BankAccountQuery.Infrastructure.Adapters.In.Web;

/// <summary>
/// 統一成功回應格式。
/// </summary>
public sealed record ApiResponse<T>(string Code, T Data, DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Success(T data) =>
        new("SUCCESS", data, DateTimeOffset.UtcNow);
}

/// <summary>
/// 統一錯誤回應格式。
/// </summary>
public sealed record ApiErrorResponse(
    string Code,
    string Message,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string[]>? Errors = null);
