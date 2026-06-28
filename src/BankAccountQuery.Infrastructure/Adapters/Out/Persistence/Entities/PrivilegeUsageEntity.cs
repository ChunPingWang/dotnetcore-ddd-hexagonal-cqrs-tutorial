using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

public sealed class PrivilegeUsageEntity
{
    public string UsageId { get; set; } = default!;
    public string PrivilegeId { get; set; } = default!;
    public DateOnly UsedDate { get; set; }
    public decimal SavedAmount { get; set; }
    public Currency Currency { get; set; }
    public string Description { get; set; } = string.Empty;

    public PrivilegeUsageRecord ToDomain() =>
        new(
            UsageId,
            UsedDate,
            new Money(SavedAmount, Currency),
            Description);
}
