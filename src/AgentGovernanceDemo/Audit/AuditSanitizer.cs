// EN: Removes credentials and personal identifiers before governance events enter durable audit storage.
// JA: ガバナンスイベントを永続監査ストレージへ保存する前に資格情報と個人識別子を除去します。

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AgentGovernance.Audit;

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Converts toolkit governance events into safe, persistable audit records.<br/>
/// JA: Toolkit のガバナンスイベントを安全に永続化できる監査レコードへ変換します。
/// </summary>
public interface IAuditSanitizer
{
    GovernanceAuditRecord Sanitize(GovernanceEvent governanceEvent);
}

/// <summary>
/// EN: Recursively redacts sensitive keys and credential-shaped text from governance events.<br/>
/// JA: ガバナンスイベントから機密キーと資格情報形式の文字列を再帰的にマスクします。
/// </summary>
/// <remarks>
/// EN: Sanitization fails closed when input cannot be represented safely as JSON.<br/>
/// JA: 入力を安全な JSON として表現できない場合は fail-closed で失敗します。
/// </remarks>
public sealed partial class AuditSanitizer : IAuditSanitizer
{
    public const string RedactedValue = "[REDACTED]";

    /// <summary>
    /// EN: Sanitizes one governance event without mutating the source object.<br/>
    /// JA: 元のオブジェクトを変更せずに 1 件のガバナンスイベントをサニタイズします。
    /// </summary>
    public GovernanceAuditRecord Sanitize(GovernanceEvent governanceEvent)
    {
        ArgumentNullException.ThrowIfNull(governanceEvent);

        if (string.IsNullOrWhiteSpace(governanceEvent.EventId))
        {
            throw new AuditSanitizationException("The governance event must have an EventId.");
        }

        if (string.IsNullOrWhiteSpace(governanceEvent.AgentId))
        {
            throw new AuditSanitizationException("The governance event must have an AgentId.");
        }

        if (string.IsNullOrWhiteSpace(governanceEvent.SessionId))
        {
            throw new AuditSanitizationException("The governance event must have a SessionId.");
        }

        try
        {
            var dataNode = JsonSerializer.SerializeToNode(
                governanceEvent.Data ?? new Dictionary<string, object>(),
                AuditJson.Options) as JsonObject
                ?? throw new AuditSanitizationException("Governance event Data must serialize as a JSON object.");

            SanitizeObject(dataNode);
            var data = dataNode.Deserialize<Dictionary<string, object?>>(AuditJson.Options)
                ?? throw new AuditSanitizationException("Governance event Data could not be sanitized.");

            return new GovernanceAuditRecord
            {
                EventId = governanceEvent.EventId,
                Timestamp = governanceEvent.Timestamp,
                Type = governanceEvent.Type,
                AgentId = governanceEvent.AgentId,
                SessionId = governanceEvent.SessionId,
                PolicyName = SanitizeText(governanceEvent.PolicyName),
                Data = data
            };
        }
        catch (AuditSanitizationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new AuditSanitizationException(
                $"Governance event '{governanceEvent.EventId}' could not be safely sanitized.",
                exception);
        }
    }

    private static void SanitizeObject(JsonObject value)
    {
        foreach (var property in value.ToArray())
        {
            if (SensitiveKeyRegex().IsMatch(property.Key))
            {
                value[property.Key] = RedactedValue;
                continue;
            }

            SanitizeNode(property.Value);
        }
    }

    private static void SanitizeNode(JsonNode? value)
    {
        switch (value)
        {
            case JsonObject jsonObject:
                SanitizeObject(jsonObject);
                break;
            case JsonArray jsonArray:
                for (var index = 0; index < jsonArray.Count; index++)
                {
                    var item = jsonArray[index];
                    if (item is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
                    {
                        jsonArray[index] = SanitizeText(text);
                    }
                    else
                    {
                        SanitizeNode(item);
                    }
                }

                break;
            case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text):
                jsonValue.ReplaceWith(SanitizeText(text));
                break;
        }
    }

    private static string? SanitizeText(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (ConnectionStringRegex().IsMatch(value))
        {
            return RedactedValue;
        }

        var sanitized = UriCredentialRegex().Replace(value, "$1" + RedactedValue + "@");
        sanitized = BearerTokenRegex().Replace(sanitized, "Bearer " + RedactedValue);
        sanitized = JwtRegex().Replace(sanitized, RedactedValue);
        sanitized = AssignedCredentialRegex().Replace(sanitized, "$1=" + RedactedValue);
        return EmailRegex().Replace(sanitized, "[REDACTED_EMAIL]");
    }

    [GeneratedRegex(
        @"(^|[_.\-\s])(password|passwd|pwd|secret|token|access[_.\-]?token|refresh[_.\-]?token|api[_.\-]?key|account[_.\-]?key|private[_.\-]?key|client[_.\-]?secret|authorization|credential|cookie|connection[_.\-]?string|sas)($|[_.\-\s])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveKeyRegex();

    [GeneratedRegex(
        @"(?:^|;)\s*(?:AccountKey|SharedAccessSignature|Password|Pwd|ClientSecret|Client_Secret|User\s*Id)\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringRegex();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(
        @"(?i)\b(api[_-]?key|access[_-]?token|client[_-]?secret|password|passwd|pwd)\s*=\s*[^;\s&,]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex AssignedCredentialRegex();

    [GeneratedRegex(@"(\b[a-z][a-z0-9+.-]*://)[^/\s:@]+:[^/\s@]+@", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UriCredentialRegex();

    [GeneratedRegex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
