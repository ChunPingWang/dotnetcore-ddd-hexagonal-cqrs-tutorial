using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankAccountQuery.Application.Queries.Account.Results;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BankAccountQuery.Infrastructure.Tests.Web;

public sealed class AccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccountControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient AuthedClient(string customerId = "C001")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.ForCustomer(customerId));
        return client;
    }

    [Fact(DisplayName = "成功查詢台幣交易紀錄應回傳 200 與 3 筆")]
    public async Task GetTwdTransactions_ValidRequest_Returns200()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/accounts/00123456789012/transactions/twd" +
            "?startDate=2025-01-01&endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<TwdTransactionHistoryResult>>();
        body!.Code.Should().Be("SUCCESS");
        body.Data.Transactions.Should().HaveCount(3);
    }

    [Fact(DisplayName = "未提供 JWT 應回傳 401")]
    public async Task GetTwdTransactions_NoAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/accounts/00123456789012/transactions/twd" +
            "?startDate=2025-01-01&endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "查詢超過 13 個月應回傳 422 與 QUERY_RANGE_EXCEEDED")]
    public async Task GetTwdTransactions_RangeExceeded_Returns422()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/accounts/00123456789012/transactions/twd" +
            "?startDate=2023-12-01&endDate=2025-02-01");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("QUERY_RANGE_EXCEEDED");
    }

    [Fact(DisplayName = "查詢不屬於自己的帳戶應回傳 403")]
    public async Task GetTwdTransactions_NotOwned_Returns403()
    {
        var client = AuthedClient("C001");

        var response = await client.GetAsync(
            "/api/v1/accounts/00999999999999/transactions/twd" +
            "?startDate=2025-01-01&endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("ACCOUNT_NOT_OWNED_BY_CUSTOMER");
    }

    [Fact(DisplayName = "凍結帳戶查詢應回傳 422 與 ACCOUNT_NOT_ACTIVE")]
    public async Task GetTwdTransactions_Frozen_Returns422()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/accounts/00123456780000/transactions/twd" +
            "?startDate=2025-01-01&endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("ACCOUNT_NOT_ACTIVE");
    }

    [Fact(DisplayName = "每頁筆數為 0 應回傳 400")]
    public async Task GetTwdTransactions_InvalidSize_Returns400()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/accounts/00123456789012/transactions/twd" +
            "?startDate=2025-01-01&endDate=2025-01-31&size=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "成功查詢外幣交易紀錄應回傳 200 並含匯率")]
    public async Task GetFxTransactions_Valid_Returns200()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/accounts/00123456789099/transactions/fx" +
            "?currency=USD&startDate=2025-01-01&endDate=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FxTransactionHistoryResult>>();
        body!.Data.CurrencyCode.Should().Be("USD");
        body.Data.Transactions.Should().NotBeEmpty();
        body.Data.Transactions[0].TwdEquivalent.Should().NotBe("-");
    }
}
