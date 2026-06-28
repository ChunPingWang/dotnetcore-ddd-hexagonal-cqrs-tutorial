using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankAccountQuery.Application.Queries.Privilege.Results;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BankAccountQuery.Infrastructure.Tests.Web;

public sealed class PrivilegeControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PrivilegeControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient AuthedClient(string customerId = "C001")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.ForCustomer(customerId));
        return client;
    }

    [Fact(DisplayName = "成功查詢轉帳優惠應回傳 200，剩餘次數為 7")]
    public async Task GetTransferPrivileges_Returns200()
    {
        var client = AuthedClient();

        var response = await client.GetAsync("/api/v1/customers/me/privileges/transfer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<TransferPrivilegeResult>>();
        body!.Data.Privileges.Should().ContainSingle();
        body.Data.Privileges[0].PrivilegeId.Should().Be("P001");
        body.Data.Privileges[0].RemainingQuota.Should().Be(7);
    }

    [Fact(DisplayName = "成功查詢優惠使用紀錄應回傳 200")]
    public async Task GetPrivilegeUsage_Returns200()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/customers/me/privileges/transfer/P001/usage" +
            "?startDate=2025-01-01&endDate=2025-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PrivilegeUsageHistoryResult>>();
        body!.Data.PrivilegeId.Should().Be("P001");
        body.Data.Records.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "查詢不屬於自己的優惠使用紀錄應回傳 403")]
    public async Task GetPrivilegeUsage_NotOwned_Returns403()
    {
        var client = AuthedClient("C001");

        var response = await client.GetAsync(
            "/api/v1/customers/me/privileges/transfer/P999/usage" +
            "?startDate=2025-01-01&endDate=2025-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("PRIVILEGE_NOT_OWNED_BY_CUSTOMER");
    }

    [Fact(DisplayName = "查詢不存在的優惠使用紀錄應回傳 404")]
    public async Task GetPrivilegeUsage_NotFound_Returns404()
    {
        var client = AuthedClient();

        var response = await client.GetAsync(
            "/api/v1/customers/me/privileges/transfer/P404/usage" +
            "?startDate=2025-01-01&endDate=2025-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("PRIVILEGE_NOT_FOUND");
    }
}
