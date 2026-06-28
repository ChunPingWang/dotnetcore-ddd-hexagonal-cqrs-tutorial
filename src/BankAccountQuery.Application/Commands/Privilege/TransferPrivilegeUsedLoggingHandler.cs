using BankAccountQuery.Application.Common;
using BankAccountQuery.Domain.Model.Privilege;
using Microsoft.Extensions.Logging;

namespace BankAccountQuery.Application.Commands.Privilege;

/// <summary>
/// 領域事件處理者範例：優惠被使用後記錄一筆日誌。
/// 實務上可在此寄送通知、更新讀模型、發布整合事件等。
/// </summary>
public sealed class TransferPrivilegeUsedLoggingHandler
    : IDomainEventHandler<TransferPrivilegeUsedEvent>
{
    private readonly ILogger<TransferPrivilegeUsedLoggingHandler> _logger;

    public TransferPrivilegeUsedLoggingHandler(
        ILogger<TransferPrivilegeUsedLoggingHandler> logger) => _logger = logger;

    public Task HandleAsync(
        TransferPrivilegeUsedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "領域事件 TransferPrivilegeUsed：客戶 {Owner} 使用優惠 {Privilege}（節省 {Saved}），剩餘 {Remaining} 次",
            domainEvent.OwnerId.Value,
            domainEvent.PrivilegeId.Value,
            domainEvent.SavedAmount,
            domainEvent.RemainingQuota);
        return Task.CompletedTask;
    }
}
