using BankAccountQuery.Application.Common;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace BankAccountQuery.Infrastructure.Tests.Persistence;

public sealed class EventSourcedPrivilegeAdapterTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // 共用同一個 InMemory 資料庫名稱，模擬不同 DbContext 看到同一份事件儲存
    private static DbContextOptions<BankDbContext> SharedOptions(InMemoryDatabaseRoot root, string name) =>
        new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(name, root)
            .Options;

    private static TransferPrivilege NewPrivilege() =>
        TransferPrivilege.Grant(
            PrivilegeId.Of("P010"), CustomerId.Of("C002"),
            PrivilegeType.FeeWaiverInterBank, 5,
            new DateRange(Today.AddYears(-1), Today.AddYears(1)));

    [Fact(DisplayName = "Grant→Use 後，重新由事件串流載入應得到一致狀態")]
    public async Task SaveThenLoad_RoundTrips()
    {
        var root = new InMemoryDatabaseRoot();
        var name = $"es-{Guid.NewGuid():N}";

        await using (var db = new BankDbContext(SharedOptions(root, name)))
        {
            var adapter = new EventSourcedPrivilegeAdapter(db);
            var p = NewPrivilege();          // Granted（Version 1）
            p.Use("U-1", Money.Twd(15m), "跨行轉帳免手續費", Today);  // Used（Version 2）
            await adapter.SaveAsync(p);
        }

        await using (var db = new BankDbContext(SharedOptions(root, name)))
        {
            var adapter = new EventSourcedPrivilegeAdapter(db);
            var reloaded = await adapter.FindByPrivilegeIdAsync(PrivilegeId.Of("P010"));

            reloaded.Should().NotBeNull();
            reloaded!.UsedQuota.Should().Be(1);
            reloaded.GetRemainingQuota().Should().Be(4);
            reloaded.Version.Should().Be(2);
            (await db.PrivilegeEvents.CountAsync()).Should().Be(2);  // granted + used
        }
    }

    [Fact(DisplayName = "依客戶查詢應由創生事件找出其 stream")]
    public async Task FindByCustomer_ResolvesStreams()
    {
        var root = new InMemoryDatabaseRoot();
        var name = $"es-{Guid.NewGuid():N}";

        await using var db = new BankDbContext(SharedOptions(root, name));
        var adapter = new EventSourcedPrivilegeAdapter(db);
        await adapter.SaveAsync(NewPrivilege());  // C002 / P010

        var list = await adapter.FindByCustomerIdAsync(CustomerId.Of("C002"));

        list.Should().ContainSingle().Which.PrivilegeId.Value.Should().Be("P010");
        (await adapter.FindByCustomerIdAsync(CustomerId.Of("C999"))).Should().BeEmpty();
    }

    [Fact(DisplayName = "兩個寫入者基於同一版本附加事件，第二者應拋出 ConcurrencyConflictException")]
    public async Task ConcurrentAppend_ThrowsConcurrencyConflict()
    {
        var root = new InMemoryDatabaseRoot();
        var name = $"es-{Guid.NewGuid():N}";

        // 先核發 P010（stream 版本 1）
        await using (var seed = new BankDbContext(SharedOptions(root, name)))
        {
            await new EventSourcedPrivilegeAdapter(seed).SaveAsync(NewPrivilege());
        }

        // 兩個寫入者各自載入版本 1
        await using var dbA = new BankDbContext(SharedOptions(root, name));
        await using var dbB = new BankDbContext(SharedOptions(root, name));
        var a = await new EventSourcedPrivilegeAdapter(dbA).FindByPrivilegeIdAsync(PrivilegeId.Of("P010"));
        var b = await new EventSourcedPrivilegeAdapter(dbB).FindByPrivilegeIdAsync(PrivilegeId.Of("P010"));

        a!.Use("U-A", Money.Twd(15m), "x", Today);
        b!.Use("U-B", Money.Twd(15m), "x", Today);

        await new EventSourcedPrivilegeAdapter(dbA).SaveAsync(a);  // 附加版本 2，成功

        var act = () => new EventSourcedPrivilegeAdapter(dbB).SaveAsync(b);  // 也想附加版本 2
        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
