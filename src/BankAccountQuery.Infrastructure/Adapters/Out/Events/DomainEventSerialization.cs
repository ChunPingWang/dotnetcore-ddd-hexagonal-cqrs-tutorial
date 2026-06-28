using System.Reflection;
using System.Text.Json;
using BankAccountQuery.Domain.Common;

namespace BankAccountQuery.Infrastructure.Adapters.Out.Events;

/// <summary>
/// 領域事件與 Outbox 內容（JSON）的轉換。
/// 型別以 FullName 儲存，並於 Domain 組件中還原。
/// </summary>
public static class DomainEventSerialization
{
    private static readonly JsonSerializerOptions Options = new();
    private static readonly Assembly DomainAssembly = typeof(IDomainEvent).Assembly;

    public static (string Type, string Content) Serialize(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType();
        return (type.FullName!, JsonSerializer.Serialize(domainEvent, type, Options));
    }

    public static IDomainEvent Deserialize(string typeName, string content)
    {
        var type = DomainAssembly.GetType(typeName)
            ?? throw new InvalidOperationException($"未知的領域事件型別：{typeName}");
        return (IDomainEvent)JsonSerializer.Deserialize(content, type, Options)!;
    }
}
