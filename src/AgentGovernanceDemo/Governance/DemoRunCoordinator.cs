// EN: Orchestrates the visible request, governance gate, tool execution, and result stages.
// JA: 画面に表示するリクエスト、ガバナンスゲート、ツール実行、結果の各段階を統括します。

namespace AgentGovernanceDemo.Governance;

// Note 1 (EN): This coordinator is the best starting point for a source-code walkthrough.
// Note 1 (JA): このコーディネーターが、ソースコードを順番に説明する際の起点です。
// Note 1 (EN): It owns the same four stages displayed by the UI and keeps policy evaluation before execution.
// Note 1 (JA): UI と同じ 4 段階を管理し、必ずポリシー評価をツール実行より前に配置します。
/// <summary>
/// EN: Coordinates one governed tool-call run and guarantees policy evaluation precedes execution.<br/>
/// JA: 1 回のガバナンス対象ツール呼び出しを調整し、ポリシー評価が実行より先に行われることを保証します。
/// </summary>
public sealed class DemoRunCoordinator
{
    private const string AgentId = "did:mesh:governance-demo-blazor-ui";
    private readonly GovernanceDemoService _governance;
    private readonly IDemoToolExecutor _tools;
    private readonly IGovernanceDemoEventSink _events;
    private long _sequence;

    public DemoRunCoordinator(
        GovernanceDemoService governance,
        IDemoToolExecutor? tools = null,
        IGovernanceDemoEventSink? events = null)
    {
        _governance = governance ?? throw new ArgumentNullException(nameof(governance));
        _tools = tools ?? new DeterministicDemoToolExecutor();
        _events = events ?? NullGovernanceDemoEventSink.Instance;
    }

    public IReadOnlyList<GovernanceScenario> Scenarios => GovernanceScenarioCatalog.All;

    /// <summary>
    /// EN: Runs a catalog scenario identified by its stable ID.<br/>
    /// JA: 安定した ID で識別されるカタログシナリオを実行します。
    /// </summary>
    public Task<DemoRunState> RunAsync(
        string scenarioId,
        CancellationToken cancellationToken = default) =>
        RunAsync(GovernanceScenarioCatalog.GetRequired(scenarioId), cancellationToken);

    /// <summary>
    /// EN: Runs all four stages for the supplied immutable scenario.<br/>
    /// JA: 指定された不変シナリオについて 4 段階すべてを実行します。
    /// </summary>
    public async Task<DemoRunState> RunAsync(
        GovernanceScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        cancellationToken.ThrowIfCancellationRequested();

        // Note 2 (EN): A new session ID correlates the visible run, live events, and audit records.
        // Note 2 (JA): 新しい session ID により、画面の実行、ライブイベント、監査レコードを相関できます。
        // Note 2 (EN): Interlocked provides a thread-safe sequence even when multiple browser sessions run.
        // Note 2 (JA): 複数ブラウザーセッションが同時実行しても、Interlocked が安全な連番を生成します。
        var sessionId = Guid.NewGuid().ToString("N");
        var runSequence = Interlocked.Increment(ref _sequence);
        var steps = new List<DemoRunStep>();

        // ---------------------------------------------------------------------
        // Note 3 - Demo Step 1: Request / デモステップ 1: リクエスト
        // Note 3 (EN): Record what the agent intends to call before making any security decision.
        // Note 3 (JA): セキュリティ判断を行う前に、エージェントが何を呼び出そうとしたかを記録します。
        // Note 3 (EN): This creates the UI's Request stage but does not execute the tool.
        // Note 3 (JA): ここでは UI の Request 段階だけを作成し、ツールはまだ実行しません。
        // ---------------------------------------------------------------------
        await AddStepAsync(
            steps,
            sessionId,
            scenario,
            DemoRunStepKind.Request,
            DemoRunStepStatus.Completed,
            "Scenario requested",
            $"{scenario.ToolName} requested for session {sessionId}.",
            cancellationToken);

        // ---------------------------------------------------------------------
        // Note 4 - Demo Step 2: Governance Gate / デモステップ 2: ガバナンスゲート
        // Note 4 (EN): Evaluate is the mandatory authorization boundary for every tool request.
        // Note 4 (JA): Evaluate は、すべてのツール要求が必ず通過する認可境界です。
        // Note 4 (EN): The decision contains both a Boolean result and an explainable policy reason.
        // Note 4 (JA): 判断結果には許可可否だけでなく、お客様へ説明できるポリシー理由も含まれます。
        // ---------------------------------------------------------------------
        var decision = _governance.Evaluate(AgentId, scenario.ToolName, scenario.Arguments);
        var decisionDetails = GovernanceDecisionDetails.Create(
            decision.Allowed,
            decision.Reason,
            decision.PolicyDecision?.MatchedRule);
        await AddStepAsync(
            steps,
            sessionId,
            scenario,
            DemoRunStepKind.PolicyEvaluation,
            decision.Allowed ? DemoRunStepStatus.Allowed : DemoRunStepStatus.Denied,
            decision.Allowed ? "Governance allowed the call" : "Governance denied the call",
            decision.Reason,
            cancellationToken);

        if (!decision.Allowed)
        {
            // Note 5 (EN): Deny is fail-closed: execution is explicitly marked Skipped and never invoked.
            // Note 5 (JA): 拒否時は fail-closed とし、実行を明示的に Skipped にして呼び出しません。
            // Note 5 (EN): This early return is the key proof that governance blocks the side effect.
            // Note 5 (JA): この早期 return が、ガバナンスによって副作用を防止する重要な証拠です。
            await AddStepAsync(
                steps,
                sessionId,
                scenario,
                DemoRunStepKind.ToolExecution,
                DemoRunStepStatus.Skipped,
                "Tool execution skipped",
                "Denied tools are never invoked.",
                cancellationToken);

            return new DemoRunState(
                sessionId,
                runSequence,
                scenario,
                DemoRunStatus.Denied,
                steps.AsReadOnly(),
                null,
                decision.Reason,
                decisionDetails);
        }

        try
        {
            // -----------------------------------------------------------------
            // Note 6 - Demo Step 3: Tool Execution / デモステップ 3: ツール実行
            // Note 6 (EN): This line is reachable only after the governance decision is Allowed.
            // Note 6 (JA): この行へ到達できるのは、ガバナンス判断が Allowed の場合だけです。
            // Note 6 (EN): The demo executor is deterministic and has no shell, file, or network side effects.
            // Note 6 (JA): デモ用 executor は決定論的で、shell、file、network の副作用を持ちません。
            // -----------------------------------------------------------------
            var output = await _tools.ExecuteAsync(
                scenario.ToolName,
                scenario.Arguments,
                cancellationToken);

            await AddStepAsync(
                steps,
                sessionId,
                scenario,
                DemoRunStepKind.ToolExecution,
                DemoRunStepStatus.Completed,
                "Simulated tool executed",
                output,
                cancellationToken);

            // -----------------------------------------------------------------
            // Note 7 - Demo Step 4: Result / デモステップ 4: 結果
            // Note 7 (EN): The approved tool output becomes the final result shown to the customer.
            // Note 7 (JA): 許可されたツール出力を、お客様に表示する最終結果として確定します。
            // Note 7 (EN): Result is recorded separately so the UI can distinguish execution from presentation.
            // Note 7 (JA): 実行処理と結果表示を区別できるよう、Result を独立した段階として記録します。
            // -----------------------------------------------------------------
            await AddStepAsync(
                steps,
                sessionId,
                scenario,
                DemoRunStepKind.Result,
                DemoRunStepStatus.Allowed,
                "Run completed",
                output,
                cancellationToken);

            return new DemoRunState(
                sessionId,
                runSequence,
                scenario,
                DemoRunStatus.Allowed,
                steps.AsReadOnly(),
                output,
                decision.Reason,
                decisionDetails);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Note 8 (EN): Operational failures become an explicit Result failure instead of a false success.
            // Note 8 (JA): 実行時エラーは成功扱いにせず、Result 段階の明示的な失敗へ変換します。
            await AddStepAsync(
                steps,
                sessionId,
                scenario,
                DemoRunStepKind.Result,
                DemoRunStepStatus.Failed,
                "Run failed",
                ex.Message,
                cancellationToken);

            return new DemoRunState(
                sessionId,
                runSequence,
                scenario,
                DemoRunStatus.Failed,
                steps.AsReadOnly(),
                null,
                decision.Reason,
                decisionDetails);
        }
    }

    private async ValueTask AddStepAsync(
        ICollection<DemoRunStep> steps,
        string sessionId,
        GovernanceScenario scenario,
        DemoRunStepKind kind,
        DemoRunStepStatus status,
        string title,
        string detail,
        CancellationToken cancellationToken)
    {
        // Note 9 (EN): Every stage is stored in the final state and published immediately for live UI updates.
        // Note 9 (JA): 各段階は最終状態へ保存されると同時に、UI のライブ更新用イベントとして配信されます。
        // Note 9 (EN): Sequence ordering lets the presentation layer select the newest event per stage.
        // Note 9 (JA): sequence 順序により、表示層は各段階の最新イベントを確実に選択できます。
        var sequence = Interlocked.Increment(ref _sequence);
        steps.Add(new DemoRunStep(sequence, kind, status, title, detail));
        await _events.PublishAsync(
            new GovernanceDemoEvent(
                sessionId,
                sequence,
                scenario.Id,
                kind,
                status,
                detail),
            cancellationToken);
    }
}
