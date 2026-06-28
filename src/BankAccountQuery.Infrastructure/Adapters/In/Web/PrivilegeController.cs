using BankAccountQuery.Application.Commands.Privilege;
using BankAccountQuery.Application.Commands.Privilege.Results;
using BankAccountQuery.Application.Queries.Privilege;
using BankAccountQuery.Application.Queries.Privilege.Results;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.In.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankAccountQuery.Infrastructure.Adapters.In.Web;

/// <summary>
/// Driving Adapter：轉帳優惠查詢端點。
/// </summary>
[ApiController]
[Route("api/v1/customers/me/privileges")]
[Authorize]
public sealed class PrivilegeController : ControllerBase
{
    private readonly ISender _sender;

    public PrivilegeController(ISender sender) => _sender = sender;

    [HttpGet("transfer")]
    [ProducesResponseType(typeof(ApiResponse<TransferPrivilegeResult>), 200)]
    public async Task<IActionResult> GetTransferPrivileges(
        CancellationToken cancellationToken = default)
    {
        var query = new GetTransferPrivilegeQuery(CustomerId.Of(User.GetCustomerId()));

        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<TransferPrivilegeResult>.Success(result));
    }

    [HttpGet("transfer/{privilegeId}/usage")]
    [ProducesResponseType(typeof(ApiResponse<PrivilegeUsageHistoryResult>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPrivilegeUsage(
        [FromRoute] string privilegeId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPrivilegeUsageHistoryQuery(
            CustomerId.Of(User.GetCustomerId()),
            PrivilegeId.Of(privilegeId),
            new DateRange(startDate, endDate),
            page,
            size);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PrivilegeUsageHistoryResult>.Success(result));
    }

    // ── 寫入側：使用一次轉帳優惠 ────────────────────────────────────────
    [HttpPost("transfer/{privilegeId}/use")]
    [ProducesResponseType(typeof(ApiResponse<UseTransferPrivilegeResult>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> UseTransferPrivilege(
        [FromRoute] string privilegeId,
        [FromBody] UsePrivilegeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UseTransferPrivilegeCommand(
            CustomerId.Of(User.GetCustomerId()),
            PrivilegeId.Of(privilegeId),
            request.SavedAmount,
            request.Description);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<UseTransferPrivilegeResult>.Success(result));
    }
}

/// <summary>使用優惠的請求內容。</summary>
public sealed record UsePrivilegeRequest(decimal SavedAmount, string Description);
