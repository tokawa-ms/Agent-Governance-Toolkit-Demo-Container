using System.ComponentModel.DataAnnotations;

namespace AgentGovernanceDemo.Configuration;

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
