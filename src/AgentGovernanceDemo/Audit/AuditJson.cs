// EN: Centralizes the JSON format shared by audit writers and readers.
// JA: 監査の書き込み側と読み取り側で共有する JSON 形式を一元管理します。

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Provides the canonical serializer options for the audit JSONL contract.<br/>
/// JA: 監査 JSONL 契約で使用する標準シリアライザー設定を提供します。
/// </summary>
internal static class AuditJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
