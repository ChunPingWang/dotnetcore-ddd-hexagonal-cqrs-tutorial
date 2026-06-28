using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.BddTests.Support;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using FluentAssertions;
using Reqnroll;

namespace BankAccountQuery.BddTests.Steps;

[Binding]
public sealed class AccountSteps
{
    private readonly ScenarioWorld _world;

    public AccountSteps(ScenarioWorld world) => _world = world;

    private static string MapType(string chinese) => chinese switch
    {
        "存入" => "Credit",
        "提出" => "Debit",
        _ => chinese
    };

    // ── 台幣 ──────────────────────────────────────────────────────────
    [When(@"查詢帳戶 ""(.*)"" 從 ""(.*)"" 到 ""(.*)"" 的台幣交易紀錄")]
    public Task WhenQueryTwd(string accountId, string startDate, string endDate)
        => _world.GetAsync(
            $"/api/v1/accounts/{accountId}/transactions/twd?startDate={startDate}&endDate={endDate}");

    [When(@"查詢帳戶 ""(.*)"" 從 ""(.*)"" 到 ""(.*)"" 每頁 ""(.*)"" 筆的台幣交易紀錄")]
    public Task WhenQueryTwdWithSize(string accountId, string startDate, string endDate, string size)
        => _world.GetAsync(
            $"/api/v1/accounts/{accountId}/transactions/twd" +
            $"?startDate={startDate}&endDate={endDate}&size={size}");

    [Then(@"應回傳 (\d+) 筆交易紀錄")]
    public void ThenTwdCountIs(int expected)
    {
        var body = _world.Deserialize<ApiResponse<TwdTransactionHistoryResult>>();
        body.Data.Transactions.Should().HaveCount(expected);
    }

    [Then(@"最近一筆交易類型為 ""(.*)"" 金額為 ""(.*)""")]
    public void ThenFirstTransaction(string type, string amount)
    {
        var body = _world.Deserialize<ApiResponse<TwdTransactionHistoryResult>>();
        var first = body.Data.Transactions[0];
        first.TransactionType.Should().Be(MapType(type));
        first.Amount.Should().Be(amount);
    }

    // ── 外幣 ──────────────────────────────────────────────────────────
    [When(@"查詢帳戶 ""(.*)"" 幣別 ""(.*)"" 從 ""(.*)"" 到 ""(.*)"" 的外幣交易紀錄")]
    public Task WhenQueryFx(string accountId, string currency, string startDate, string endDate)
        => _world.GetAsync(
            $"/api/v1/accounts/{accountId}/transactions/fx" +
            $"?currency={currency}&startDate={startDate}&endDate={endDate}");

    [Then(@"應回傳 (\d+) 筆外幣交易紀錄")]
    public void ThenFxCountIs(int expected)
    {
        var body = _world.Deserialize<ApiResponse<FxTransactionHistoryResult>>();
        body.Data.Transactions.Should().HaveCount(expected);
    }

    [Then(@"每筆外幣交易皆顯示台幣等值與匯率")]
    public void ThenEachFxHasTwdEquivalent()
    {
        var body = _world.Deserialize<ApiResponse<FxTransactionHistoryResult>>();
        body.Data.Transactions.Should().NotBeEmpty();
        body.Data.Transactions.Should().OnlyContain(
            t => t.TwdEquivalent != "-" && t.ExchangeRate != "-");
    }
}
