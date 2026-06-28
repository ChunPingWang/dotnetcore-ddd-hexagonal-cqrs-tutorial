using BankAccountQuery.BddTests.Support;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using FluentAssertions;
using Reqnroll;

namespace BankAccountQuery.BddTests.Steps;

[Binding]
public sealed class CommonSteps
{
    private readonly ScenarioWorld _world;

    public CommonSteps(ScenarioWorld world) => _world = world;

    [Given(@"已完成身份認證的客戶 ""(.*)""")]
    public void GivenAuthenticatedCustomer(string customerId)
        => _world.AuthenticateAs(customerId);

    [Given(@"客戶尚未登入")]
    public void GivenNotAuthenticated()
        => _world.ClearAuthentication();

    [Then(@"回應狀態碼為 (\d+)")]
    public void ThenStatusCodeIs(int expected)
        => ((int)_world.Response!.StatusCode).Should().Be(expected);

    [Then(@"錯誤代碼為 ""(.*)""")]
    public void ThenErrorCodeIs(string expected)
    {
        var error = _world.Deserialize<ApiErrorResponse>();
        error.Code.Should().Be(expected);
    }
}
