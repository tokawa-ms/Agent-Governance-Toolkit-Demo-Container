using System.Threading.Channels;
using AgentGovernance;
using AgentGovernance.Audit;
using AgentGovernanceDemo.Audit;
using AgentGovernanceDemo.Governance;
using AgentGovernanceDemo.Telemetry;

namespace AgentGovernanceDemo.Integration;

public sealed class GovernanceAuditPersistenceWorker : BackgroundService
{
    private const int QueueCapacity = 1_024;
    private readonly GovernanceDemoService _governance;
    private readonly BlobAuditSink _sink;
    private readonly DemoTelemetry _telemetry;
    private readonly StorageHealthMonitor _storageHealth;
    private readonly ILogger<GovernanceAuditPersistenceWorker> _logger;
    private readonly Channel<GovernanceEvent> _queue = Channel.CreateBounded<GovernanceEvent>(
        new BoundedChannelOptions(QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public GovernanceAuditPersistenceWorker(
        GovernanceDemoService governance,
        BlobAuditSink sink,
        DemoTelemetry telemetry,
        StorageHealthMonitor storageHealth,
        ILogger<GovernanceAuditPersistenceWorker> logger)
    {
        _governance = governance;
        _sink = sink;
        _telemetry = telemetry;
        _storageHealth = storageHealth;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _governance.Kernel.OnAllEvents(QueueEvent);

        await foreach (var governanceEvent in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await PersistWithRetryAsync(governanceEvent, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.TryComplete();
        return base.StopAsync(cancellationToken);
    }

    private void QueueEvent(GovernanceEvent governanceEvent)
    {
        var context = DemoRunContext.Current;
        if (context is null)
        {
            _logger.LogWarning(
                "Governance event {EventId} had no active demo session and was not queued.",
                governanceEvent.EventId);
            return;
        }

        var correlatedEvent = new GovernanceEvent
        {
            EventId = governanceEvent.EventId,
            Timestamp = governanceEvent.Timestamp,
            Type = governanceEvent.Type,
            AgentId = governanceEvent.AgentId,
            SessionId = context.AuditSessionId,
            PolicyName = governanceEvent.PolicyName,
            Data = governanceEvent.Data is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(governanceEvent.Data)
        };

        if (!_queue.Writer.TryWrite(correlatedEvent))
        {
            const string message = "監査キューが満杯のためイベントを受け付けられませんでした。";
            _storageHealth.MarkFailed(message);
            _logger.LogError(
                "Audit queue capacity {Capacity} was exceeded; event {EventId} was rejected.",
                QueueCapacity,
                governanceEvent.EventId);
        }
    }

    private async Task PersistWithRetryAsync(
        GovernanceEvent governanceEvent,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var telemetry = _telemetry.StartBlobAppend();
            try
            {
                await _sink.PersistAsync(governanceEvent, cancellationToken);
                telemetry.Complete(TelemetryOutcome.Succeeded);
                _storageHealth.MarkHealthy();
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (AuditSanitizationException exception)
            {
                telemetry.Complete(TelemetryOutcome.Failed);
                _storageHealth.MarkFailed("監査イベントの安全なサニタイズに失敗しました。");
                _logger.LogError(
                    exception,
                    "Audit event {EventId} failed sanitization and will not be retried.",
                    governanceEvent.EventId);
                return;
            }
            catch (Exception exception)
            {
                telemetry.Complete(TelemetryOutcome.Failed);
                lastError = exception;
                _logger.LogWarning(
                    exception,
                    "Audit append attempt {Attempt} failed for event {EventId}.",
                    attempt,
                    governanceEvent.EventId);
                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                }
            }
        }

        _storageHealth.MarkFailed("認証または Blob Append に失敗しました。");
        _logger.LogError(
            lastError,
            "Audit event {EventId} could not be persisted after retries.",
            governanceEvent.EventId);
    }
}
