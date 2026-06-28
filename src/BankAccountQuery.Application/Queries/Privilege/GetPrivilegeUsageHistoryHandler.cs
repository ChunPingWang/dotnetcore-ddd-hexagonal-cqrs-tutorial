using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Privilege.Results;
using BankAccountQuery.Domain.Exceptions;
using MediatR;

namespace BankAccountQuery.Application.Queries.Privilege;

public sealed class GetPrivilegeUsageHistoryHandler
    : IRequestHandler<GetPrivilegeUsageHistoryQuery, PrivilegeUsageHistoryResult>
{
    private readonly ILoadPrivilegePort _loadPrivilegePort;

    public GetPrivilegeUsageHistoryHandler(ILoadPrivilegePort loadPrivilegePort)
        => _loadPrivilegePort = loadPrivilegePort;

    public async Task<PrivilegeUsageHistoryResult> Handle(
        GetPrivilegeUsageHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var privilege = await _loadPrivilegePort.FindByPrivilegeIdAsync(
            query.PrivilegeId, cancellationToken)
            ?? throw new PrivilegeNotFoundException(query.PrivilegeId);

        privilege.VerifyOwnership(query.CustomerId);                  // Domain 執行

        var usageHistory = privilege.FilterUsageHistory(query.DateRange); // Domain 執行

        return PrivilegeUsageHistoryResult.From(usageHistory, query.Page, query.Size);
    }
}
