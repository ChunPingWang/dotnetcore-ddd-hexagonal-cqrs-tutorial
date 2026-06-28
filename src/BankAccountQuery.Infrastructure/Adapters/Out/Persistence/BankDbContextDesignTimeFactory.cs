using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// 設計階段（dotnet ef migrations）使用的 DbContext 工廠。
/// 固定使用 Npgsql，以便產生關聯式 Migration（與執行階段的供應者設定無關）。
/// 連線字串可由環境變數 BANKDB_CONNECTION 覆寫。
/// </summary>
public sealed class BankDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BankDbContext>
{
    public BankDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("BANKDB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=bankdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BankDbContext(options);
    }
}
