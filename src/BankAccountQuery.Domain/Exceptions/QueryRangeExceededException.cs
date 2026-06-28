namespace BankAccountQuery.Domain.Exceptions;

public sealed class QueryRangeExceededException : DomainException
{
    public QueryRangeExceededException(string message) : base(message) { }
}
