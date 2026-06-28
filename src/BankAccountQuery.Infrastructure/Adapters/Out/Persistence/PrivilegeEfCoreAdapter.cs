using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Exceptions;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Driven Adapter：以 EF Core 實作 ILoadPrivilegePort（讀）與 ISavePrivilegePort（寫）。
/// </summary>
public sealed class PrivilegeEfCoreAdapter : ILoadPrivilegePort, ISavePrivilegePort
{
    private readonly BankDbContext _dbContext;

    public PrivilegeEfCoreAdapter(BankDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<TransferPrivilege>> FindByCustomerIdAsync(
        CustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Privileges
            .AsNoTracking()
            .Include(p => p.UsageRecords)
            .Where(p => p.OwnerId == customerId.Value)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<TransferPrivilege?> FindByPrivilegeIdAsync(
        PrivilegeId privilegeId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Privileges
            .AsNoTracking()
            .Include(p => p.UsageRecords)
            .FirstOrDefaultAsync(p => p.PrivilegeId == privilegeId.Value, cancellationToken);

        return entity?.ToDomain();
    }

    // ── 寫入側：持久化聚合的狀態變更（更新次數 + 新增使用紀錄）────────────
    public async Task SaveAsync(
        TransferPrivilege privilege,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Privileges
            .Include(p => p.UsageRecords)
            .FirstOrDefaultAsync(p => p.PrivilegeId == privilege.PrivilegeId.Value, cancellationToken)
            ?? throw new PrivilegeNotFoundException(privilege.PrivilegeId);

        entity.UsedQuota = privilege.UsedQuota;

        var existingUsageIds = entity.UsageRecords.Select(u => u.UsageId).ToHashSet();
        foreach (var record in privilege.UsageRecords.Where(r => !existingUsageIds.Contains(r.UsageId)))
        {
            entity.UsageRecords.Add(new PrivilegeUsageEntity
            {
                UsageId = record.UsageId,
                PrivilegeId = privilege.PrivilegeId.Value,
                UsedDate = record.UsedDate,
                SavedAmount = record.SavedAmount.Amount,
                Currency = record.SavedAmount.Currency,
                Description = record.Description
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
