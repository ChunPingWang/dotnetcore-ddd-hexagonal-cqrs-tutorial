using Microsoft.AspNetCore.Mvc.Testing;

namespace BankAccountQuery.BddTests.Support;

/// <summary>
/// 整個測試執行共用同一個 WebApplicationFactory 主機（避免重複播種共用 InMemory DB）。
/// 各情境只建立各自的 HttpClient。
/// </summary>
public static class TestHost
{
    private static readonly Lazy<WebApplicationFactory<Program>> LazyFactory =
        new(() => new WebApplicationFactory<Program>());

    public static WebApplicationFactory<Program> Factory => LazyFactory.Value;
}
