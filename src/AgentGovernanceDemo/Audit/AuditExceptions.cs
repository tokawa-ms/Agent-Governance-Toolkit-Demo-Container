namespace AgentGovernanceDemo.Audit;

public sealed class AuditSanitizationException : Exception
{
    public AuditSanitizationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AuditPersistenceException : Exception
{
    public AuditPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class AuditReadException : Exception
{
    public AuditReadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
