using FluentValidation;

namespace BankAccountQuery.Application.Commands.Privilege;

public sealed class UseTransferPrivilegeCommandValidator
    : AbstractValidator<UseTransferPrivilegeCommand>
{
    public UseTransferPrivilegeCommandValidator()
    {
        RuleFor(c => c.PrivilegeId)
            .NotNull().WithMessage("優惠 ID 不可為空");

        RuleFor(c => c.SavedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("節省金額不可為負數");

        RuleFor(c => c.Description)
            .NotEmpty().WithMessage("使用說明不可為空");
    }
}
