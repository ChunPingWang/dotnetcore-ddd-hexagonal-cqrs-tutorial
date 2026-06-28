namespace BankAccountQuery.Domain.Model.Shared;

/// <summary>
/// 幣別 Value Object（以 Enum 表達，封裝可支援幣別）。
/// </summary>
public enum Currency
{
    TWD,
    USD,
    JPY,
    EUR,
    CNY,
    GBP,
    AUD,
    HKD
}
