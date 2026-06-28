using FluentValidation.Results;

namespace BankAccountQuery.Application.Common;

/// <summary>
/// Query 參數驗證失敗（由 ValidationBehavior 拋出）。
/// 對應 HTTP 400。
/// </summary>
public sealed class QueryValidationException : Exception
{
    public IReadOnlyList<ValidationFailure> Failures { get; }

    public QueryValidationException(IReadOnlyList<ValidationFailure> failures)
        : base("Query 參數驗證失敗：" +
               string.Join("; ", failures.Select(f => f.ErrorMessage)))
    {
        Failures = failures;
    }

    public IReadOnlyDictionary<string, string[]> Errors =>
        Failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
}
