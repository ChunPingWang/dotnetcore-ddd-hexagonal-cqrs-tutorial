namespace BankAccountQuery.Domain.Exceptions;

/// <summary>
/// 所有 Domain Exception 的抽象基底類別。
/// 使用業務語意命名，不含技術細節。
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
