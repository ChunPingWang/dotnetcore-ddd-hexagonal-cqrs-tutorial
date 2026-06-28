using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Model.Account;
using BankAccountQuery.Domain.Model.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Driven Adapter：以 EF Core 實作 ILoadTransactionPort（DB 層初步區間過濾）。
/// </summary>
public sealed class TransactionEfCoreAdapter : ILoadTransactionPort
{
    private readonly BankDbContext _dbContext;

    public TransactionEfCoreAdapter(BankDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<Transaction>> FindByAccountIdAsync(
        AccountId accountId,
        DateRange dateRange,
        CancellationToken cancellationToken = default)
    {
        var start = dateRange.StartDate.ToDateTime(TimeOnly.MinValue);
        var endExclusive = dateRange.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var entities = await _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId.Value
                        && t.TransactionDate >= start
                        && t.TransactionDate < endExclusive)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }
}
