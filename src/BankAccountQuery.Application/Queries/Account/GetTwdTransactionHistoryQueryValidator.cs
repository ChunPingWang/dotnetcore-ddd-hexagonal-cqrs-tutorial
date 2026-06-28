using FluentValidation;

namespace BankAccountQuery.Application.Queries.Account;

public sealed class GetTwdTransactionHistoryQueryValidator
    : AbstractValidator<GetTwdTransactionHistoryQuery>
{
    public GetTwdTransactionHistoryQueryValidator()
    {
        RuleFor(q => q.AccountId)
            .NotNull().WithMessage("帳號不可為空");

        RuleFor(q => q.DateRange)
            .NotNull().WithMessage("查詢區間不可為空");

        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(0).WithMessage("頁碼不可為負數");

        RuleFor(q => q.Size)
            .InclusiveBetween(1, 100).WithMessage("每頁筆數需介於 1 至 100");
    }
}
