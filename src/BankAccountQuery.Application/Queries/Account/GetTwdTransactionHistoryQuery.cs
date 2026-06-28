using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using MediatR;

namespace BankAccountQuery.Application.Queries.Account;

public sealed record GetTwdTransactionHistoryQuery(
    CustomerId CustomerId,
    AccountId AccountId,
    DateRange DateRange,
    int Page,
    int Size
) : IRequest<TwdTransactionHistoryResult>, ICustomerQuery;
