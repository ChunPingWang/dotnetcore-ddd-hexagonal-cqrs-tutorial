using BankAccountQuery.Domain.Model.Privilege;

namespace BankAccountQuery.Application.Queries.Privilege.Results;

public sealed record PrivilegeUsageDto(
    string UsageId,
    string UsedDate,
    string CurrencyCode,
    string SavedAmount,
    string Description)
{
    public static PrivilegeUsageDto From(PrivilegeUsageRecord r) => new(
        r.UsageId,
        r.UsedDate.ToString("yyyy-MM-dd"),
        r.SavedAmount.Currency.ToString(),
        r.SavedAmount.Amount.ToString("N2"),
        r.Description);
}
