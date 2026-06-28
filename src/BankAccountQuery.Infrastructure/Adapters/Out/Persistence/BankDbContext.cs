using BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence;

public sealed class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<PrivilegeEntity> Privileges => Set<PrivilegeEntity>();
    public DbSet<PrivilegeUsageEntity> PrivilegeUsages => Set<PrivilegeUsageEntity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PrivilegeEventEntity> PrivilegeEvents => Set<PrivilegeEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(e =>
        {
            e.HasKey(x => x.AccountId);
            e.Property(x => x.OwnerId).IsRequired();
        });

        modelBuilder.Entity<TransactionEntity>(e =>
        {
            e.HasKey(x => x.TransactionId);
            e.HasIndex(x => x.AccountId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TwdEquivalent).HasPrecision(18, 2);
            // 交易日期不帶時區語意，避免 Npgsql 將 DateTime 對應為 timestamptz（要求 UTC）
            e.Property(x => x.TransactionDate).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<PrivilegeEntity>(e =>
        {
            e.HasKey(x => x.PrivilegeId);
            e.HasIndex(x => x.OwnerId);
            e.HasMany(x => x.UsageRecords)
             .WithOne()
             .HasForeignKey(u => u.PrivilegeId);
        });

        modelBuilder.Entity<PrivilegeUsageEntity>(e =>
        {
            e.HasKey(x => x.UsageId);
            e.Property(x => x.SavedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProcessedOnUtc);   // 加速「尚未處理」查詢
            // OccurredOnUtc / ProcessedOnUtc 皆為 UTC，對應 timestamptz（接受 Kind=Utc）
            e.Property(x => x.OccurredOnUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.ProcessedOnUtc).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<PrivilegeEventEntity>(e =>
        {
            e.HasKey(x => new { x.StreamId, x.Version });   // 複合主鍵 = 樂觀並行
            e.Property(x => x.OccurredOnUtc).HasColumnType("timestamp with time zone");
        });
    }
}
