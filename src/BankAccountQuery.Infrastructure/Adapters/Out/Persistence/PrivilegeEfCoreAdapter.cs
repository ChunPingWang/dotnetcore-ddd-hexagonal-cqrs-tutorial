using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Driven Adapter：以 EF Core 實作 ILoadPrivilegePort。
/// </summary>
public sealed class PrivilegeEfCoreAdapter : ILoadPrivilegePort
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
}
