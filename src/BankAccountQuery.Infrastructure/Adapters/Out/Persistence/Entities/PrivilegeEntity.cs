using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Domain.Model.Shared;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Entities;

public sealed class PrivilegeEntity
{
    public string PrivilegeId { get; set; } = default!;
    public string OwnerId { get; set; } = default!;
    public PrivilegeType Type { get; set; }
    public int TotalQuota { get; set; }
    public int UsedQuota { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }

    public List<PrivilegeUsageEntity> UsageRecords { get; set; } = new();

    public TransferPrivilege ToDomain() =>
        new(
            Domain.Model.Privilege.PrivilegeId.Of(PrivilegeId),
            CustomerId.Of(OwnerId),
            Type,
            TotalQuota,
            UsedQuota,
            new DateRange(ValidFrom, ValidTo),
            UsageRecords.Select(u => u.ToDomain()).ToList());
}
