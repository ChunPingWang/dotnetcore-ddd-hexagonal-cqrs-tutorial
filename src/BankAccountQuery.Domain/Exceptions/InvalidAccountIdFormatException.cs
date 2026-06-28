namespace BankAccountQuery.Domain.Exceptions;

public sealed class InvalidAccountIdFormatException : DomainException
{
    public InvalidAccountIdFormatException(string message) : base(message) { }
}
