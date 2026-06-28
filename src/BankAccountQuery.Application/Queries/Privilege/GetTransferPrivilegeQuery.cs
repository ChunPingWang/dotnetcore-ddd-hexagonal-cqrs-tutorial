using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Queries.Privilege.Results;
using BankAccountQuery.Domain.Model.Shared;
using MediatR;

namespace BankAccountQuery.Application.Queries.Privilege;

public sealed record GetTransferPrivilegeQuery(
    CustomerId CustomerId
) : IRequest<TransferPrivilegeResult>, ICustomerQuery;
