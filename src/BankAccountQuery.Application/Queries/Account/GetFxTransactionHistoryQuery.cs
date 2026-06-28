using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using MediatR;

namespace BankAccountQuery.Application.Queries.Account;

public sealed record GetFxTransactionHistoryQuery(
    CustomerId CustomerId,
    AccountId AccountId,
    Currency Currency,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<FxTransactionHistoryResult>, ICustomerQuery;
