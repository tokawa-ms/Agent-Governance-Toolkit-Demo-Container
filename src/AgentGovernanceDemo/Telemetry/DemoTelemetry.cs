using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgentGovernanceDemo.Telemetry;

public enum TelemetryOutcome
{
    Succeeded,
    Failed
}

public enum PolicyEvaluationOutcome
{
    Allowed,
    Denied,
    Failed
}

public sealed class DemoTelemetry
{
    public const string ActivitySourceName = "AgentGovernanceDemo";
    public const string MeterName = "AgentGovernanceDemo";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> DemoRuns = Meter.CreateCounter<long>("agent_governance_demo.runs");
    private static readonly Histogram<double> DemoRunDuration = Meter.CreateHistogram<double>(
        "agent_governance_demo.run.duration",
        "s");
    private static readonly Counter<long> PolicyEvaluations = Meter.CreateCounter<long>(
        "agent_governance_demo.policy_evaluations");
    private static readonly Histogram<double> PolicyEvaluationDuration = Meter.CreateHistogram<double>(
        "agent_governance_demo.policy_evaluation.duration",
        "s");
    private static readonly Counter<long> BlobAppends = Meter.CreateCounter<long>(
        "agent_governance_demo.blob_appends");
    private static readonly Histogram<double> BlobAppendDuration = Meter.CreateHistogram<double>(
        "agent_governance_demo.blob_append.duration",
        "s");

    public DemoRunTelemetryScope StartDemoRun() =>
        new(ActivitySource.StartActivity("demo.run", ActivityKind.Internal));

    public PolicyEvaluationTelemetryScope StartPolicyEvaluation() =>
        new(ActivitySource.StartActivity("governance.policy.evaluate", ActivityKind.Internal));

    public BlobAppendTelemetryScope StartBlobAppend() =>
        new(ActivitySource.StartActivity("audit.blob.append", ActivityKind.Client));

    public sealed class DemoRunTelemetryScope : TelemetryScope
    {
        internal DemoRunTelemetryScope(Activity? activity)
            : base(activity)
        {
        }

        public void Complete(TelemetryOutcome outcome)
        {
            var tag = OutcomeTag(outcome);
            if (!TryCompleteCore(tag))
            {
                return;
            }

            DemoRuns.Add(1, new KeyValuePair<string, object?>("outcome", tag));
            DemoRunDuration.Record(
                Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("outcome", tag));
        }
    }

    public sealed class PolicyEvaluationTelemetryScope : TelemetryScope
    {
        internal PolicyEvaluationTelemetryScope(Activity? activity)
            : base(activity)
        {
        }

        public void Complete(PolicyEvaluationOutcome outcome)
        {
            var tag = outcome switch
            {
                PolicyEvaluationOutcome.Allowed => "allowed",
                PolicyEvaluationOutcome.Denied => "denied",
                _ => "failed"
            };

            if (!TryCompleteCore(tag, outcome == PolicyEvaluationOutcome.Failed))
            {
                return;
            }

            PolicyEvaluations.Add(1, new KeyValuePair<string, object?>("outcome", tag));
            PolicyEvaluationDuration.Record(
                Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("outcome", tag));
        }
    }

    public sealed class BlobAppendTelemetryScope : TelemetryScope
    {
        internal BlobAppendTelemetryScope(Activity? activity)
            : base(activity)
        {
        }

        public void Complete(TelemetryOutcome outcome)
        {
            var tag = OutcomeTag(outcome);
            if (!TryCompleteCore(tag))
            {
                return;
            }

            BlobAppends.Add(1, new KeyValuePair<string, object?>("outcome", tag));
            BlobAppendDuration.Record(
                Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("outcome", tag));
        }
    }

    public abstract class TelemetryScope : IDisposable
    {
        private readonly Activity? _activity;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private bool _completed;

        protected TelemetryScope(Activity? activity)
        {
            _activity = activity;
        }

        protected TimeSpan Elapsed => _stopwatch.Elapsed;

        protected bool TryCompleteCore(string outcome, bool failed = false)
        {
            if (_completed)
            {
                return false;
            }

            _completed = true;
            _stopwatch.Stop();
            _activity?.SetTag("outcome", outcome);
            _activity?.SetStatus(failed ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
            return true;
        }

        public void Dispose()
        {
            if (!_completed)
            {
                TryCompleteCore("failed", failed: true);
            }

            _activity?.Dispose();
        }
    }

    private static string OutcomeTag(TelemetryOutcome outcome) =>
        outcome == TelemetryOutcome.Succeeded ? "succeeded" : "failed";
}
