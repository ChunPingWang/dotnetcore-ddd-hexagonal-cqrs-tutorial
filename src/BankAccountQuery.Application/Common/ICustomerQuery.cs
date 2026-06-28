using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Application.Common;

/// <summary>
/// Marker Interface：所有含 CustomerId 的 Query 實作此介面，
/// 讓 AuditLogBehavior 能識別並擷取 CustomerId（ISP 實踐）。
/// </summary>
public interface ICustomerQuery
{
    CustomerId CustomerId { get; }
}
