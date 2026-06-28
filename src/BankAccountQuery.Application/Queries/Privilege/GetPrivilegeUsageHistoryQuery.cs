using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Queries.Privilege.Results;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using MediatR;

namespace BankAccountQuery.Application.Queries.Privilege;

public sealed record GetPrivilegeUsageHistoryQuery(
    CustomerId CustomerId,
    PrivilegeId PrivilegeId,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<PrivilegeUsageHistoryResult>, ICustomerQuery;
