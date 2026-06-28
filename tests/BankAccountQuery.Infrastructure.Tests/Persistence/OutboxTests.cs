using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Domain.Common;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.Out.Events;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BankAccountQuery.Infrastructure.Tests.Persistence;

public sealed class OutboxTests
{
    private static BankDbContext NewDb() =>
        new(new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase($"outbox-{Guid.NewGuid():N}")
            .Options);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static TransferPrivilege ValidPrivilege() =>
        new(PrivilegeId.Of("P010"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, 5, 0,
            new DateRange(Today.AddYears(-1), Today.AddYears(1)));

    [Fact(DisplayName = "使用優惠並儲存後，領域事件應寫入 Outbox（與狀態同一交易）")]
    public async Task SaveAsync_WritesDomainEventToOutbox()
    {
        await using var db = NewDb();
        db.Privileges.Add(new PrivilegeEntity
        {
            PrivilegeId = "P010", OwnerId = "C002",
            Type = PrivilegeType.FeeWaiverInterBank, TotalQuota = 5, UsedQuota = 0,
            ValidFrom = Today.AddYears(-1), ValidTo = Today.AddYears(1)
        });
        await db.SaveChangesAsync();

        var adapter = new PrivilegeEfCoreAdapter(db);
        var privilege = ValidPrivilege();
        privilege.Use("U-1", Money.Twd(15m), "跨行轉帳免手續費", Today);

        await adapter.SaveAsync(privilege);

        var outbox = await db.OutboxMessages.SingleAsync();
        outbox.ProcessedOnUtc.Should().BeNull();
        outbox.Type.Should().Be(typeof(TransferPrivilegeUsedEvent).FullName);
        outbox.Content.Should().Contain("P010");

        // 狀態與事件在同一次儲存：UsedQuota 已加計
        (await db.Privileges.SingleAsync()).UsedQuota.Should().Be(1);
    }

    [Fact(DisplayName = "OutboxProcessor 應派發未處理訊息並標記 ProcessedOnUtc")]
    public async Task ProcessPending_DispatchesAndMarksProcessed()
    {
        await using var db = NewDb();
        var (type, content) = DomainEventSerialization.Serialize(new TransferPrivilegeUsedEvent(
            PrivilegeId.Of("P010"), CustomerId.Of("C002"), "U-1",
            Money.Twd(15m), "跨行轉帳免手續費", 4, Today, DateTime.UtcNow));
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type, Content = content,
            OccurredOnUtc = DateTime.UtcNow, ProcessedOnUtc = null
        });
        await db.SaveChangesAsync();

        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(db, dispatcher, NullLogger<OutboxProcessor>.Instance);

        var processed = await processor.ProcessPendingAsync();

        processed.Should().Be(1);
        dispatcher.Dispatched.Should().ContainSingle()
            .Which.Should().BeOfType<TransferPrivilegeUsedEvent>();
        (await db.OutboxMessages.SingleAsync()).ProcessedOnUtc.Should().NotBeNull();
    }

    [Fact(DisplayName = "已處理的訊息不應重複派發")]
    public async Task ProcessPending_SkipsAlreadyProcessed()
    {
        await using var db = NewDb();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = "done", Type = "x", Content = "{}",
            OccurredOnUtc = DateTime.UtcNow, ProcessedOnUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dispatcher = new RecordingDispatcher();
        var processor = new OutboxProcessor(db, dispatcher, NullLogger<OutboxProcessor>.Instance);

        var processed = await processor.ProcessPendingAsync();

        processed.Should().Be(0);
        dispatcher.Dispatched.Should().BeEmpty();
    }

    private sealed class RecordingDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = new();

        public Task DispatchAsync(
            IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
