namespace AgentGovernanceDemo.Integration;

public enum StorageHealthState
{
    Unverified,
    Healthy,
    Failed
}

public sealed record StorageHealthSnapshot(StorageHealthState State, string Message);

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
