// EN: Declares domain-specific exceptions for audit sanitization, persistence, and reading failures.
// JA: 監査のサニタイズ、永続化、読み取り失敗を表すドメイン固有例外を宣言します。

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Represents a failure to convert a governance event into a safe audit record.<br/>
/// JA: ガバナンスイベントを安全な監査レコードへ変換できなかったことを表します。
/// </summary>
public sealed class AuditSanitizationException : Exception
{
    public AuditSanitizationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// EN: Represents a failure while serializing or storing an audit record.<br/>
/// JA: 監査レコードのシリアライズまたは保存中の失敗を表します。
/// </summary>
public sealed class AuditPersistenceException : Exception
{
    public AuditPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// EN: Represents a failure while decoding or reading persisted audit data.<br/>
/// JA: 永続化された監査データのデコードまたは読み取り中の失敗を表します。
/// </summary>
public sealed class AuditReadException : Exception
{
    public AuditReadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
