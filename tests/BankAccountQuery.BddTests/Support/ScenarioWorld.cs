using System.Net.Http.Headers;
using System.Text.Json;

namespace BankAccountQuery.BddTests.Support;

/// <summary>
/// 每個情境一份的共享狀態（Reqnroll 以建構子注入到各 Steps 類別）。
/// 保存目前的 HttpClient、最後一次回應與其內容字串。
/// </summary>
public sealed class ScenarioWorld
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client = TestHost.Factory.CreateClient();

    public string CurrentCustomerId { get; private set; } = string.Empty;
    public HttpResponseMessage? Response { get; private set; }
    public string Body { get; private set; } = string.Empty;

    public void AuthenticateAs(string customerId)
    {
        CurrentCustomerId = customerId;
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.ForCustomer(customerId));
    }

    public void ClearAuthentication()
    {
        CurrentCustomerId = string.Empty;
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task GetAsync(string url)
    {
        Response = await _client.GetAsync(url);
        Body = await Response.Content.ReadAsStringAsync();
    }

    public async Task PostJsonAsync(string url, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        Response = await _client.PostAsync(url, content);
        Body = await Response.Content.ReadAsStringAsync();
    }

    public T Deserialize<T>() =>
        JsonSerializer.Deserialize<T>(Body, JsonOptions)
        ?? throw new InvalidOperationException($"無法將回應反序列化為 {typeof(T).Name}：{Body}");
}
