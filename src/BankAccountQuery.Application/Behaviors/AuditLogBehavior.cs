using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Ports.Out;
using MediatR;

namespace BankAccountQuery.Application.Behaviors;

/// <summary>
/// 對含 CustomerId 的 Query 統一寫入稽核日誌。
/// 來源 IP 透過 IRequestContextPort 取得，避免依賴 ASP.NET Core。
/// </summary>
public sealed class AuditLogBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogPort _auditLogPort;
    private readonly IRequestContextPort _requestContext;

    public AuditLogBehavior(
        IAuditLogPort auditLogPort,
        IRequestContextPort requestContext)
    {
        _auditLogPort = auditLogPort;
        _requestContext = requestContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // 只記錄實作 ICustomerQuery 的 Query
        if (request is ICustomerQuery customerQuery)
        {
            await _auditLogPort.RecordAsync(new AuditLogEntry(
                CustomerId: customerQuery.CustomerId.Value,
                QueryType: typeof(TRequest).Name,
                IpAddress: _requestContext.GetClientIpAddress(),
                Timestamp: DateTime.UtcNow
            ), cancellationToken);
        }

        return response;
    }
}
