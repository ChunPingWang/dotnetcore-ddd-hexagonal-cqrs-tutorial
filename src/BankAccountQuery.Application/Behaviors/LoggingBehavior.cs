using MediatR;
using Microsoft.Extensions.Logging;

namespace BankAccountQuery.Application.Behaviors;

/// <summary>
/// 記錄 Query 開始 / 結束 / 耗時。
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var queryName = typeof(TRequest).Name;
        _logger.LogInformation("處理 Query：{QueryName}", queryName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("完成 Query：{QueryName}，耗時 {ElapsedMs}ms",
                queryName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query 失敗：{QueryName}，耗時 {ElapsedMs}ms",
                queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
