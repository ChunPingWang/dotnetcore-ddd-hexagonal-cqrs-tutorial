using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// 領域事件：轉帳優惠被使用了一次。
/// 內含重建使用紀錄所需的完整資訊（含 Description），以支援事件溯源重播。
/// </summary>
public sealed record TransferPrivilegeUsedEvent(
    PrivilegeId PrivilegeId,
    CustomerId OwnerId,
    string UsageId,
    Money SavedAmount,
    string Description,
    int RemainingQuota,
    DateOnly UsedDate,
    DateTime OccurredOn) : IDomainEvent;
