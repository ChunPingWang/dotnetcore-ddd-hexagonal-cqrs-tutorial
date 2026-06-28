using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankAccountQuery.Application.Commands.Privilege.Results;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BankAccountQuery.Infrastructure.Tests.Web;

public sealed class UseTransferPrivilegeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UseTransferPrivilegeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient AuthedClient(string customerId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.ForCustomer(customerId));
        return client;
    }

    [Fact(DisplayName = "成功使用優惠應回傳 200 並扣減剩餘次數")]
    public async Task Use_Valid_Returns200AndDecrements()
    {
        var client = AuthedClient("C002");

        var response = await client.PostAsync(
            "/api/v1/customers/me/privileges/transfer/P010/use",
            JsonContent.Create(new { savedAmount = 15m, description = "跨行轉帳免手續費" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<UseTransferPrivilegeResult>>();
        body!.Data.PrivilegeId.Should().Be("P010");
        body.Data.RemainingQuota.Should().Be(4); // 種子 total 5 - 1
        body.Data.UsageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "次數用盡的優惠使用應回傳 422 PRIVILEGE_QUOTA_EXHAUSTED")]
    public async Task Use_Exhausted_Returns422()
    {
        var client = AuthedClient("C002");

        var response = await client.PostAsync(
            "/api/v1/customers/me/privileges/transfer/P012/use",
            JsonContent.Create(new { savedAmount = 15m, description = "x" }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("PRIVILEGE_QUOTA_EXHAUSTED");
    }

    [Fact(DisplayName = "已過期的優惠使用應回傳 422 PRIVILEGE_EXPIRED")]
    public async Task Use_Expired_Returns422()
    {
        var client = AuthedClient("C002");

        var response = await client.PostAsync(
            "/api/v1/customers/me/privileges/transfer/P013/use",
            JsonContent.Create(new { savedAmount = 15m, description = "x" }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("PRIVILEGE_EXPIRED");
    }

    [Fact(DisplayName = "使用不屬於自己的優惠應回傳 403")]
    public async Task Use_NotOwned_Returns403()
    {
        var client = AuthedClient("C002"); // P999 屬於 C999

        var response = await client.PostAsync(
            "/api/v1/customers/me/privileges/transfer/P999/use",
            JsonContent.Create(new { savedAmount = 15m, description = "x" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body!.Code.Should().Be("PRIVILEGE_NOT_OWNED_BY_CUSTOMER");
    }
}
