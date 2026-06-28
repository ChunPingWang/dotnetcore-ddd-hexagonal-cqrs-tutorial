using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Privilege.Results;
using MediatR;

namespace BankAccountQuery.Application.Queries.Privilege;

public sealed class GetTransferPrivilegeHandler
    : IRequestHandler<GetTransferPrivilegeQuery, TransferPrivilegeResult>
{
    private readonly ILoadPrivilegePort _loadPrivilegePort;

    public GetTransferPrivilegeHandler(ILoadPrivilegePort loadPrivilegePort)
        => _loadPrivilegePort = loadPrivilegePort;

    public async Task<TransferPrivilegeResult> Handle(
        GetTransferPrivilegeQuery query,
        CancellationToken cancellationToken)
    {
        var privileges = await _loadPrivilegePort.FindByCustomerIdAsync(
            query.CustomerId, cancellationToken);

        // 每個 Aggregate 自己計算業務狀態（IsValid、GetRemainingQuota）
        return TransferPrivilegeResult.From(privileges);
    }
}
