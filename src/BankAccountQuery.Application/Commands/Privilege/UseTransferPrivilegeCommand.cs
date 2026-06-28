using BankAccountQuery.Application.Commands.Privilege.Results;
using BankAccountQuery.Application.Common;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using MediatR;

namespace BankAccountQuery.Application.Commands.Privilege;

/// <summary>
/// Command（寫入側）：使用一次轉帳優惠。
/// 與 Query 相同實作 IRequest&lt;TResult&gt;，但會改變系統狀態。
/// 實作 ICustomerQuery 讓 AuditLogBehavior 一併留存稽核日誌。
/// </summary>
public sealed record UseTransferPrivilegeCommand(
    CustomerId CustomerId,
    PrivilegeId PrivilegeId,
    decimal SavedAmount,
    string Description
) : IRequest<UseTransferPrivilegeResult>, ICustomerQuery;
