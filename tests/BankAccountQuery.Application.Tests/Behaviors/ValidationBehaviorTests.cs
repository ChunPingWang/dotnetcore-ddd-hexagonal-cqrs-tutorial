using BankAccountQuery.Application.Behaviors;
using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Queries.Account;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Application.Tests.Fixtures;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace BankAccountQuery.Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static TwdTransactionHistoryResult EmptyResult() =>
        new("", Array.Empty<TwdTransactionDto>(), PageInfo.Empty);

    [Fact(DisplayName = "Query 參數不合法應拋出 QueryValidationException")]
    public async Task Handle_InvalidQuery_ThrowsQueryValidationException()
    {
        var validators = new List<IValidator<GetTwdTransactionHistoryQuery>>
        {
            new GetTwdTransactionHistoryQueryValidator()
        };
        var behavior = new ValidationBehavior<GetTwdTransactionHistoryQuery,
                                              TwdTransactionHistoryResult>(validators);

        // size = 0 違反 InclusiveBetween(1, 100)
        var invalidQuery = QueryFixture.TwdQuery("C001", "00123456789012", size: 0);

        var act = () => behavior.Handle(
            invalidQuery,
            () => Task.FromResult(EmptyResult()),
            CancellationToken.None);

        await act.Should().ThrowAsync<QueryValidationException>();
    }

    [Fact(DisplayName = "Query 參數合法應呼叫下一個處理者")]
    public async Task Handle_ValidQuery_CallsNext()
    {
        var validators = new List<IValidator<GetTwdTransactionHistoryQuery>>
        {
            new GetTwdTransactionHistoryQueryValidator()
        };
        var behavior = new ValidationBehavior<GetTwdTransactionHistoryQuery,
                                              TwdTransactionHistoryResult>(validators);

        var validQuery = QueryFixture.TwdQuery("C001", "00123456789012", size: 20);
        var nextCalled = false;

        var result = await behavior.Handle(
            validQuery,
            () => { nextCalled = true; return Task.FromResult(EmptyResult()); },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().NotBeNull();
    }
}
