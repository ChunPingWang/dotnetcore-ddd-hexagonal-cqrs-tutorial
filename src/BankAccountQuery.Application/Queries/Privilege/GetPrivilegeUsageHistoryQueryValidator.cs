using FluentValidation;

namespace BankAccountQuery.Application.Queries.Privilege;

public sealed class GetPrivilegeUsageHistoryQueryValidator
    : AbstractValidator<GetPrivilegeUsageHistoryQuery>
{
    public GetPrivilegeUsageHistoryQueryValidator()
    {
        RuleFor(q => q.PrivilegeId)
            .NotNull().WithMessage("優惠 ID 不可為空");

        RuleFor(q => q.DateRange)
            .NotNull().WithMessage("查詢區間不可為空");

        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(0).WithMessage("頁碼不可為負數");

        RuleFor(q => q.Size)
            .InclusiveBetween(1, 100).WithMessage("每頁筆數需介於 1 至 100");
    }
}
