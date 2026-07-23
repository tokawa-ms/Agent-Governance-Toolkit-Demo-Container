// EN: Defines validated Azure Blob Storage settings for durable audit persistence and retrieval.
// JA: 監査の永続化と取得に使用する検証済み Azure Blob Storage 設定を定義します。

using System.ComponentModel.DataAnnotations;

namespace AgentGovernanceDemo.Configuration;

/// <summary>
/// EN: Provides strongly typed configuration for the audit storage account and retention view.<br/>
/// JA: 監査ストレージアカウントと表示保持件数の厳密に型付けされた構成を提供します。
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required, Url]
    public string AccountUri { get; init; } = "https://devstoreaccount1.blob.core.windows.net/";

    [Required]
    public string AuditContainerName { get; init; } = "agt-audit";

    [Range(1, 500)]
    public int RecentRecordLimit { get; init; } = 100;
}
