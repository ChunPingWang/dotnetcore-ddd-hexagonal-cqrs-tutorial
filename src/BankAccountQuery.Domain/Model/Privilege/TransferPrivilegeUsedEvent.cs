using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// 領域事件：轉帳優惠被使用了一次。
/// </summary>
public sealed record TransferPrivilegeUsedEvent(
    PrivilegeId PrivilegeId,
    CustomerId OwnerId,
    string UsageId,
    Money SavedAmount,
    int RemainingQuota,
    DateOnly UsedDate,
    DateTime OccurredOn) : IDomainEvent;
