using BankAccountQuery.Application.Common;
using BankAccountQuery.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BankAccountQuery.Infrastructure.Adapters.In.Web;

/// <summary>
/// 全域例外處理（.NET 8+ IExceptionHandler）。
/// 將 Domain / Validation Exception 映射為 HTTP 狀態碼。
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorCode) = exception switch
        {
            AccountNotFoundException                => (404, "ACCOUNT_NOT_FOUND"),
            AccountNotOwnedByCustomerException       => (403, "ACCOUNT_NOT_OWNED_BY_CUSTOMER"),
            AccountNotActiveException                => (422, "ACCOUNT_NOT_ACTIVE"),
            QueryRangeExceededException              => (422, "QUERY_RANGE_EXCEEDED"),
            InvalidAccountIdFormatException          => (400, "INVALID_ACCOUNT_ID_FORMAT"),
            QueryValidationException                 => (400, "VALIDATION_FAILED"),
            PrivilegeNotFoundException               => (404, "PRIVILEGE_NOT_FOUND"),
            PrivilegeNotOwnedByCustomerException     => (403, "PRIVILEGE_NOT_OWNED_BY_CUSTOMER"),
            PrivilegeExpiredException                => (422, "PRIVILEGE_EXPIRED"),
            PrivilegeQuotaExhaustedException         => (422, "PRIVILEGE_QUOTA_EXHAUSTED"),
            ArgumentException                        => (400, "INVALID_ARGUMENT"),
            _                                        => (500, "INTERNAL_SERVER_ERROR")
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "未預期的系統錯誤");
        else
            _logger.LogWarning("業務例外：{ErrorCode} - {Message}", errorCode, exception.Message);

        var errors = exception is QueryValidationException qve ? qve.Errors : null;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            Code: errorCode,
            Message: exception.Message,
            Timestamp: DateTimeOffset.UtcNow,
            Errors: errors), cancellationToken);

        return true;
    }
}
