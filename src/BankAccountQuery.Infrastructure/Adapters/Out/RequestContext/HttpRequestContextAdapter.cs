using BankAccountQuery.Application.Ports.Out;
using Microsoft.AspNetCore.Http;

namespace BankAccountQuery.Infrastructure.Adapters.Out.RequestContext;

/// <summary>
/// Driven Adapter：以 IHttpContextAccessor 實作 IRequestContextPort，
/// 讓 Application Layer 取得來源 IP 而不依賴 ASP.NET Core。
/// </summary>
public sealed class HttpRequestContextAdapter : IRequestContextPort
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestContextAdapter(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? GetClientIpAddress() =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
