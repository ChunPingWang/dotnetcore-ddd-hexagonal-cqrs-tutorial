namespace BankAccountQuery.Application.Ports.Out;

/// <summary>
/// Output Port：取得目前請求的環境資訊（例如來源 IP）。
/// 由 Infrastructure 以 IHttpContextAccessor 實作，
/// 讓 Application Layer 不直接依賴 ASP.NET Core。
/// </summary>
public interface IRequestContextPort
{
    string? GetClientIpAddress();
}
