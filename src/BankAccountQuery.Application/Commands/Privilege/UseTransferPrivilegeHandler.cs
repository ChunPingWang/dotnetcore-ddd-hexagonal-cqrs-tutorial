using BankAccountQuery.Application.Commands.Privilege.Results;
using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Shared;
using MediatR;

namespace BankAccountQuery.Application.Commands.Privilege;

/// <summary>
/// 寫入側 Handler：只做協調，所有不變量由聚合 <c>TransferPrivilege.Use()</c> 守護。
/// 流程：載入聚合 → 驗證所有權 → 委派 Use（變更狀態 + 產生事件）
///       → 持久化 → 派發領域事件。
/// </summary>
public sealed class UseTransferPrivilegeHandler
    : IRequestHandler<UseTransferPrivilegeCommand, UseTransferPrivilegeResult>
{
    private readonly ILoadPrivilegePort _loadPrivilegePort;
    private readonly ISavePrivilegePort _savePrivilegePort;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UseTransferPrivilegeHandler(
        ILoadPrivilegePort loadPrivilegePort,
        ISavePrivilegePort savePrivilegePort,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _loadPrivilegePort = loadPrivilegePort;
        _savePrivilegePort = savePrivilegePort;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<UseTransferPrivilegeResult> Handle(
        UseTransferPrivilegeCommand command,
        CancellationToken cancellationToken)
    {
        var privilege = await _loadPrivilegePort.FindByPrivilegeIdAsync(
            command.PrivilegeId, cancellationToken)
            ?? throw new PrivilegeNotFoundException(command.PrivilegeId);

        privilege.VerifyOwnership(command.CustomerId);   // Domain 執行

        var usageId = Guid.NewGuid().ToString("N");
        var usedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // 不變量守護 + 狀態變更 + 產生領域事件，全在聚合內完成
        var record = privilege.Use(
            usageId, Money.Twd(command.SavedAmount), command.Description, usedDate);

        // 持久化（以聚合根為單位）
        await _savePrivilegePort.SaveAsync(privilege, cancellationToken);

        // 取出事件快照後清空，再派發（避免將聚合的即時集合外洩給消費者）
        var domainEvents = privilege.DomainEvents.ToList();
        privilege.ClearDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return new UseTransferPrivilegeResult(
            privilege.PrivilegeId.Value,
            record.UsageId,
            privilege.GetRemainingQuota(),
            privilege.IsValid());
    }
}
