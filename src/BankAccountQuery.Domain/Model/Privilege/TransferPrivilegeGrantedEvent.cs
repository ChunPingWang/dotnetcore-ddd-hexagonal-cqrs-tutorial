using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Domain.Model.Privilege;

/// <summary>
/// 領域事件（創生）：核發一筆轉帳優惠。
/// 事件溯源中每個 stream 的第一個事件，建立聚合的初始狀態。
/// </summary>
public sealed record TransferPrivilegeGrantedEvent(
    PrivilegeId PrivilegeId,
    CustomerId OwnerId,
    PrivilegeType Type,
    int TotalQuota,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    DateTime OccurredOn) : IDomainEvent;
