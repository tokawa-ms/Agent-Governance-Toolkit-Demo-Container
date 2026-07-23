// EN: Maintains the latest user-facing health state for audit Blob Storage operations.
// JA: 監査 Blob Storage 操作に関する最新のユーザー向け正常性状態を維持します。

namespace AgentGovernanceDemo.Integration;

/// <summary>
/// EN: Identifies whether audit storage is unverified, healthy, or unavailable.<br/>
/// JA: 監査ストレージが未確認、正常、利用不可のいずれかを識別します。
/// </summary>
public enum StorageHealthState
{
    Unverified,
    Healthy,
    Failed
}

/// <summary>
/// EN: Captures an immutable storage-health state and display message.<br/>
/// JA: 不変のストレージ正常性状態と表示メッセージを記録します。
/// </summary>
public sealed record StorageHealthSnapshot(StorageHealthState State, string Message);

/// <summary>
/// EN: Stores thread-safe audit-storage health and notifies UI observers when it changes.<br/>
/// JA: 監査ストレージの正常性をスレッドセーフに保持し、変更時に UI 監視者へ通知します。
/// </summary>
public sealed class StorageHealthMonitor
{
    private readonly object _gate = new();
    private StorageHealthSnapshot _current = new(
        StorageHealthState.Unverified,
        "Blob Storage への接続はまだ確認されていません。");

    public event Action<StorageHealthSnapshot>? Changed;

    public StorageHealthSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void MarkHealthy() =>
        Update(new StorageHealthSnapshot(
            StorageHealthState.Healthy,
            "監査 Blob へのアクセスが確認できました。"));

    public void MarkReadChecked() =>
        Update(new StorageHealthSnapshot(
            StorageHealthState.Unverified,
            "監査 Blob は見つかりませんでした。書き込み状態は未確認です。"));

    public void MarkFailed(string message) =>
        Update(new StorageHealthSnapshot(
            StorageHealthState.Failed,
            $"Blob Storage を利用できません: {message}"));

    private void Update(StorageHealthSnapshot snapshot)
    {
        lock (_gate)
        {
            _current = snapshot;
        }

        Changed?.Invoke(snapshot);
    }
}
