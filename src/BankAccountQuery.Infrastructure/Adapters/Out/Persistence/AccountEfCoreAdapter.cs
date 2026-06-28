using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Driven Adapter：以 EF Core 實作 ILoadAccountPort。
/// </summary>
public sealed class AccountEfCoreAdapter : ILoadAccountPort
{
    private readonly BankDbContext _dbContext;

    public AccountEfCoreAdapter(BankDbContext dbContext) => _dbContext = dbContext;

    public async Task<Account?> FindByAccountIdAsync(
        AccountId accountId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == accountId.Value, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Account>> FindAllByCustomerIdAsync(
        CustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.OwnerId == customerId.Value)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }
}
