using BankAccountQuery.Application.Commands.Privilege.Results;
using BankAccountQuery.Application.Queries.Privilege.Results;
using BankAccountQuery.BddTests.Support;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using FluentAssertions;
using Reqnroll;

namespace BankAccountQuery.BddTests.Steps;

[Binding]
public sealed class PrivilegeSteps
{
    private readonly ScenarioWorld _world;

    public PrivilegeSteps(ScenarioWorld world) => _world = world;

    [When(@"查詢轉帳優惠內容")]
    public Task WhenQueryPrivileges()
        => _world.GetAsync("/api/v1/customers/me/privileges/transfer");

    [Then(@"應回傳 (\d+) 項轉帳優惠")]
    public void ThenPrivilegeCountIs(int expected)
    {
        var body = _world.Deserialize<ApiResponse<TransferPrivilegeResult>>();
        body.Data.Privileges.Should().HaveCount(expected);
    }

    [Then(@"優惠 ""(.*)"" 的剩餘次數為 (\d+)")]
    public void ThenRemainingQuotaIs(string privilegeId, int remaining)
    {
        var body = _world.Deserialize<ApiResponse<TransferPrivilegeResult>>();
        var privilege = body.Data.Privileges.Should()
            .ContainSingle(p => p.PrivilegeId == privilegeId).Subject;
        privilege.RemainingQuota.Should().Be(remaining);
    }

    [When(@"查詢優惠 ""(.*)"" 從 ""(.*)"" 到 ""(.*)"" 的使用紀錄")]
    public Task WhenQueryUsage(string privilegeId, string startDate, string endDate)
        => _world.GetAsync(
            $"/api/v1/customers/me/privileges/transfer/{privilegeId}/usage" +
            $"?startDate={startDate}&endDate={endDate}");

    [Then(@"應回傳至少 (\d+) 筆使用紀錄")]
    public void ThenUsageAtLeast(int min)
    {
        var body = _world.Deserialize<ApiResponse<PrivilegeUsageHistoryResult>>();
        body.Data.Records.Count.Should().BeGreaterThanOrEqualTo(min);
    }

    // ── 寫入側：使用優惠 ─────────────────────────────────────────────
    [When(@"客戶使用優惠 ""(.*)"" 並節省 ""(.*)"" 元，說明 ""(.*)""")]
    public Task WhenUsePrivilege(string privilegeId, decimal saved, string description)
        => _world.PostJsonAsync(
            $"/api/v1/customers/me/privileges/transfer/{privilegeId}/use",
            new { savedAmount = saved, description });

    [Then(@"優惠使用後剩餘次數為 (\d+)")]
    public void ThenRemainingAfterUse(int remaining)
    {
        var body = _world.Deserialize<ApiResponse<UseTransferPrivilegeResult>>();
        body.Data.RemainingQuota.Should().Be(remaining);
    }
}
