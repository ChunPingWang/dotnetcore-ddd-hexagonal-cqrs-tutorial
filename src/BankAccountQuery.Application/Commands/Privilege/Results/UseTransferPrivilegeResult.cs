namespace BankAccountQuery.Application.Commands.Privilege.Results;

/// <summary>
/// 使用優惠後的結果（回傳新的剩餘次數與有效狀態）。
/// </summary>
public sealed record UseTransferPrivilegeResult(
    string PrivilegeId,
    string UsageId,
    int RemainingQuota,
    bool IsValid);
