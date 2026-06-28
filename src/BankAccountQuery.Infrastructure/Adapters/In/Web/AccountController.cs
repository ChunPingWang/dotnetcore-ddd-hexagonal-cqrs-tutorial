using BankAccountQuery.Application.Queries.Account;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.In.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankAccountQuery.Infrastructure.Adapters.In.Web;

/// <summary>
/// Driving Adapter：帳戶交易紀錄查詢端點。Controller 只負責 HTTP 轉換。
/// </summary>
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly ISender _sender;

    public AccountController(ISender sender) => _sender = sender;

    [HttpGet("{accountId}/transactions/twd")]
    [ProducesResponseType(typeof(ApiResponse<TwdTransactionHistoryResult>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GetTwdTransactions(
        [FromRoute] string accountId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTwdTransactionHistoryQuery(
            CustomerId.Of(User.GetCustomerId()),
            new AccountId(accountId),
            new DateRange(startDate, endDate),
            page,
            size);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<TwdTransactionHistoryResult>.Success(result));
    }

    [HttpGet("{accountId}/transactions/fx")]
    [ProducesResponseType(typeof(ApiResponse<FxTransactionHistoryResult>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GetFxTransactions(
        [FromRoute] string accountId,
        [FromQuery] string currency,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFxTransactionHistoryQuery(
            CustomerId.Of(User.GetCustomerId()),
            new AccountId(accountId),
            Enum.Parse<Currency>(currency, ignoreCase: true),
            new DateRange(startDate, endDate),
            page,
            size);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<FxTransactionHistoryResult>.Success(result));
    }
}
