using System.Globalization;

namespace AgentGovernanceDemo.Localization;

public sealed class UiLocalizer
{
    public const string DefaultCulture = "ja-JP";

    public static readonly IReadOnlyList<string> SupportedCultures =
        ["en-US", "ja-JP", "zh-TW", "zh-CN", "zh-HK", "ko-KR"];

    private static readonly IReadOnlyDictionary<string, string[]> Text =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Language"] = ["Language", "言語", "語言", "语言", "語言", "언어"],
            ["Overview"] = ["Overview", "概要", "概覽", "概览", "概覽", "개요"],
            ["Demo"] = ["Demo", "デモ", "示範", "演示", "示範", "데모"],
            ["Pages"] = ["Pages", "ページ", "頁面", "页面", "頁面", "페이지"],
            ["PublicSafeDemo"] = ["Public safe demo", "公開用安全デモ", "公開安全示範", "公开安全演示", "公開安全示範", "공개 안전 데모"],
            ["ErrorOccurred"] = ["An error occurred.", "エラーが発生しました。", "發生錯誤。", "发生错误。", "發生錯誤。", "오류가 발생했습니다."],
            ["Reload"] = ["Reload", "再読み込み", "重新載入", "重新加载", "重新載入", "새로 고침"],
            ["Dismiss"] = ["Dismiss", "閉じる", "關閉", "关闭", "關閉", "닫기"],
            ["HomeTitle"] = ["Agent Governance Toolkit overview", "Agent Governance Toolkit デモ概要", "Agent Governance Toolkit 示範概覽", "Agent Governance Toolkit 演示概览", "Agent Governance Toolkit 示範概覽", "Agent Governance Toolkit 데모 개요"],
            ["HomeHeading"] = ["Track governance decisions stage by stage", "ガバナンス判断を段階ごとに追跡", "逐階段追蹤治理決策", "逐阶段跟踪治理决策", "逐階段追蹤管治決策", "단계별 거버넌스 결정 추적"],
            ["HomeDescription"] = ["Use fixed scenarios to inspect Request, Governance Gate, Tool Execution, and Result in real time. Denied runs show the applied gate and C# path explaining why the tool was not called.", "固定シナリオを使い、Request、Governance Gate、Tool Execution、Result の 4 段階をリアルタイムに確認できます。拒否された実行では、適用ゲートと C# コードパスを表示し、ツールが呼び出されなかった理由を説明します。", "使用固定情境即時檢視 Request、Governance Gate、Tool Execution 與 Result 四個階段。遭拒的執行會顯示套用的閘道與 C# 路徑，說明為何未呼叫工具。", "使用固定场景实时查看 Request、Governance Gate、Tool Execution 和 Result 四个阶段。被拒绝的运行会显示应用的网关与 C# 路径，说明为何未调用工具。", "使用固定情境即時檢視 Request、Governance Gate、Tool Execution 及 Result 四個階段。被拒絕的執行會顯示套用的閘道及 C# 路徑，說明為何未有呼叫工具。", "고정 시나리오로 Request, Governance Gate, Tool Execution, Result의 4단계를 실시간 확인합니다. 거부된 실행은 적용된 게이트와 C# 경로를 표시해 도구가 호출되지 않은 이유를 설명합니다."],
            ["StartDemo"] = ["Start demo", "デモを開始", "開始示範", "开始演示", "開始示範", "데모 시작"],
            ["NewTab"] = ["(opens in a new tab)", "（新しいタブで開きます）", "（在新分頁開啟）", "（在新标签页打开）", "（在新分頁開啟）", "(새 탭에서 열림)"],
            ["FlowPreview"] = ["Execution flow available in the demo", "デモで確認できる実行フロー", "示範中可檢視的執行流程", "演示中可查看的执行流程", "示範中可檢視的執行流程", "데모에서 확인할 수 있는 실행 흐름"],
            ["FailClosed"] = ["Evaluate before execution and fail closed on denial", "実行前に評価し、拒否時は fail-closed", "執行前評估，拒絕時採 fail-closed", "执行前评估，拒绝时采用 fail-closed", "執行前評估，拒絕時採 fail-closed", "실행 전 평가하고 거부 시 fail-closed"],
            ["DemoTitle"] = ["Agent Governance Toolkit demo", "Agent Governance Toolkit デモ", "Agent Governance Toolkit 示範", "Agent Governance Toolkit 演示", "Agent Governance Toolkit 示範", "Agent Governance Toolkit 데모"],
            ["DemoHeading"] = ["Governance execution demo", "ガバナンス実行デモ", "治理執行示範", "治理执行演示", "管治執行示範", "거버넌스 실행 데모"],
            ["ServiceStatus"] = ["Service status", "サービス状態", "服務狀態", "服务状态", "服務狀態", "서비스 상태"],
            ["AuditStorage"] = ["Audit storage", "監査ストレージ", "稽核儲存體", "审计存储", "審計儲存體", "감사 스토리지"],
            ["Telemetry"] = ["Telemetry", "テレメトリ", "遙測", "遥测", "遙測", "텔레메트리"],
            ["Healthy"] = ["Healthy", "正常", "正常", "正常", "正常", "정상"],
            ["Failed"] = ["Failed", "失敗", "失敗", "失败", "失敗", "실패"],
            ["Unverified"] = ["Unverified", "未確認", "未確認", "未确认", "未確認", "미확인"],
            ["Enabled"] = ["Enabled", "有効", "已啟用", "已启用", "已啟用", "사용"],
            ["Disabled"] = ["Disabled", "無効", "已停用", "已禁用", "已停用", "사용 안 함"],
            ["ConfigurationError"] = ["Configuration error", "構成エラー", "設定錯誤", "配置错误", "設定錯誤", "구성 오류"],
            ["Workspace"] = ["Governance demo workspace", "ガバナンスデモ ワークスペース", "治理示範工作區", "治理演示工作区", "管治示範工作區", "거버넌스 데모 작업 영역"],
            ["WorkspaceViews"] = ["Workspace views", "表示するワークスペース", "工作區檢視", "工作区视图", "工作區檢視", "작업 영역 보기"],
            ["AuditLog"] = ["Audit Log", "監査ログ", "稽核記錄", "审计日志", "審計記錄", "감사 로그"],
            ["Items"] = ["{0} items", "{0} 件", "{0} 項", "{0} 项", "{0} 項", "{0}개"],
            ["ScenarioSummary"] = ["Fixed scenarios and run summary", "固定シナリオと実行概要", "固定情境與執行摘要", "固定场景与运行摘要", "固定情境及執行摘要", "고정 시나리오 및 실행 요약"],
            ["RunSelected"] = ["Run selected scenario", "選択したシナリオを実行", "執行所選情境", "运行所选场景", "執行所選情境", "선택한 시나리오 실행"],
            ["RunningEllipsis"] = ["Running…", "実行中…", "執行中…", "运行中…", "執行中…", "실행 중…"],
            ["Cancel"] = ["Cancel", "キャンセル", "取消", "取消", "取消", "취소"],
            ["ScenarioToRun"] = ["Scenario to run", "実行するシナリオ", "要執行的情境", "要运行的场景", "要執行的情境", "실행할 시나리오"],
            ["ExpectedAllow"] = ["Expected allow", "許可予定", "預期允許", "预计允许", "預期允許", "허용 예정"],
            ["ExpectedDeny"] = ["Expected deny", "拒否予定", "預期拒絕", "预计拒绝", "預期拒絕", "거부 예정"],
            ["RunSummary"] = ["Run summary", "実行概要", "執行摘要", "运行摘要", "執行摘要", "실행 요약"],
            ["Scenario"] = ["Scenario", "シナリオ", "情境", "场景", "情境", "시나리오"],
            ["Decision"] = ["Decision", "判断", "決策", "决策", "決策", "결정"],
            ["Result"] = ["Result", "結果", "結果", "结果", "結果", "결과"],
            ["Running"] = ["Running", "実行中", "執行中", "运行中", "執行中", "실행 중"],
            ["Allowed"] = ["Allowed", "許可", "允許", "允许", "允許", "허용"],
            ["Denied"] = ["Denied", "拒否", "拒絕", "拒绝", "拒絕", "거부"],
            ["Waiting"] = ["Waiting", "待機中", "待命", "等待", "等候", "대기 중"],
            ["AwaitingPolicy"] = ["Waiting for policy evaluation.", "ポリシー評価を待機中です。", "正在等待原則評估。", "正在等待策略评估。", "正在等候原則評估。", "정책 평가를 기다리는 중입니다."],
            ["NotRun"] = ["Not run yet.", "まだ実行されていません。", "尚未執行。", "尚未运行。", "尚未執行。", "아직 실행되지 않았습니다."],
            ["BlockedNoTool"] = ["Blocked by governance; the tool was not executed.", "ガバナンスによりブロックされ、ツールは実行されませんでした。", "已被治理機制封鎖，工具未執行。", "已被治理机制阻止，工具未执行。", "已被管治機制封鎖，工具未執行。", "거버넌스에 의해 차단되어 도구가 실행되지 않았습니다."],
            ["InProgress"] = ["Execution is in progress.", "実行中です。", "正在執行。", "正在运行。", "正在執行。", "실행 중입니다."],
            ["NoOutput"] = ["No output yet.", "まだ出力はありません。", "尚無輸出。", "尚无输出。", "尚無輸出。", "아직 출력이 없습니다."],
            ["SecretNotice"] = ["Connection strings and credentials are never written to the UI, telemetry, or audit records.", "接続文字列や資格情報は画面・テレメトリ・監査レコードへ出力しません。", "連線字串與認證資訊不會輸出到畫面、遙測或稽核記錄。", "连接字符串和凭据不会输出到界面、遥测或审计日志。", "連線字串及認證資料不會輸出到畫面、遙測或審計記錄。", "연결 문자열과 자격 증명은 화면, 텔레메트리 또는 감사 레코드에 출력되지 않습니다."],
            ["RateLimit"] = ["Run limit reached. Try again in about {0} seconds.", "実行回数の上限に達しました。約 {0} 秒後に再試行してください。", "已達執行次數上限，請約 {0} 秒後重試。", "已达到运行次数上限，请约 {0} 秒后重试。", "已達執行次數上限，請約 {0} 秒後再試。", "실행 횟수 제한에 도달했습니다. 약 {0}초 후 다시 시도하세요."],
            ["RunAllowedNotice"] = ["Policy allowed the request and the fixed tool completed.", "ポリシーで許可され、固定ツールの実行が完了しました。", "原則已允許要求，固定工具執行完成。", "策略已允许请求，固定工具运行完成。", "原則已允許要求，固定工具執行完成。", "정책이 요청을 허용했고 고정 도구 실행이 완료되었습니다."],
            ["RunDeniedNotice"] = ["Policy denied the request. The tool was not executed.", "ポリシーで拒否されました。ツールは実行されていません。", "原則已拒絕要求，工具未執行。", "策略已拒绝请求，工具未执行。", "原則已拒絕要求，工具未執行。", "정책이 요청을 거부했습니다. 도구는 실행되지 않았습니다."],
            ["RunCancelled"] = ["The run was cancelled.", "実行をキャンセルしました。", "執行已取消。", "运行已取消。", "執行已取消。", "실행을 취소했습니다."],
            ["UserStopped"] = ["The user stopped the run.", "ユーザーが実行を停止しました。", "使用者已停止執行。", "用户已停止运行。", "使用者已停止執行。", "사용자가 실행을 중지했습니다."],
            ["RunFailure"] = ["Run failed: {0}", "実行に失敗しました: {0}", "執行失敗：{0}", "运行失败：{0}", "執行失敗：{0}", "실행 실패: {0}"],
            ["BlobReadFailure"] = ["Authentication or Blob read failed.", "認証または Blob 読み取りに失敗しました。", "驗證或 Blob 讀取失敗。", "身份验证或 Blob 读取失败。", "驗證或 Blob 讀取失敗。", "인증 또는 Blob 읽기에 실패했습니다."],
            ["WeatherTitle"] = ["Get weather", "天気情報を取得", "取得天氣資訊", "获取天气信息", "取得天氣資訊", "날씨 정보 가져오기"],
            ["TimeTitle"] = ["Get UTC time", "UTC 時刻を取得", "取得 UTC 時間", "获取 UTC 时间", "取得 UTC 時間", "UTC 시간 가져오기"],
            ["LocationTitle"] = ["Get demo location", "デモ位置情報を取得", "取得示範位置", "获取演示位置", "取得示範位置", "데모 위치 가져오기"],
            ["ShellTitle"] = ["Attempt shell execution", "シェル実行を試行", "嘗試執行 Shell", "尝试执行 Shell", "嘗試執行 Shell", "셸 실행 시도"],
            ["UnknownTitle"] = ["Attempt unknown tool", "未登録ツールを試行", "嘗試未知工具", "尝试未知工具", "嘗試未知工具", "알 수 없는 도구 시도"],
            ["InjectionTitle"] = ["Detect prompt injection", "プロンプトインジェクションを検出", "偵測提示詞注入", "检测提示词注入", "偵測提示詞注入", "프롬프트 인젝션 탐지"],
            ["WeatherDescription"] = ["Returns fixed, read-only weather data.", "読み取り専用の固定天気データを返します。", "傳回固定的唯讀天氣資料。", "返回固定的只读天气数据。", "傳回固定的唯讀天氣資料。", "읽기 전용 고정 날씨 데이터를 반환합니다."],
            ["TimeDescription"] = ["Returns fixed UTC time data.", "固定された UTC 時刻データを返します。", "傳回固定的 UTC 時間資料。", "返回固定的 UTC 时间数据。", "傳回固定的 UTC 時間資料。", "고정 UTC 시간 데이터를 반환합니다."],
            ["LocationDescription"] = ["Returns fixed demo location data.", "固定されたデモ用位置情報を返します。", "傳回固定的示範位置資料。", "返回固定的演示位置数据。", "傳回固定的示範位置資料。", "고정 데모 위치 데이터를 반환합니다."],
            ["ShellDescription"] = ["Verifies an explicit deny rule. No shell exists.", "明示的な拒否ルールを確認します。シェルは存在しません。", "驗證明確拒絕規則；Shell 並不存在。", "验证显式拒绝规则；Shell 并不存在。", "驗證明確拒絕規則；Shell 並不存在。", "명시적 거부 규칙을 확인합니다. 셸은 존재하지 않습니다."],
            ["UnknownDescription"] = ["Default deny blocks an unregistered tool.", "Default deny により未登録ツールを拒否します。", "Default deny 會封鎖未註冊工具。", "Default deny 会阻止未注册工具。", "Default deny 會封鎖未註冊工具。", "Default deny가 등록되지 않은 도구를 차단합니다."],
            ["InjectionDescription"] = ["Detects fixed hostile text before execution.", "敵対的な固定文字列を実行前に検出します。", "在執行前偵測固定的惡意文字。", "在执行前检测固定的恶意文本。", "在執行前偵測固定的惡意文字。", "실행 전에 고정된 악성 텍스트를 탐지합니다."],
            ["ExecutionFlow"] = ["Execution flow", "実行フロー", "執行流程", "执行流程", "執行流程", "실행 흐름"],
            ["RequestToResult"] = ["Request to result", "リクエストから結果まで", "從要求到結果", "从请求到结果", "由要求到結果", "요청부터 결과까지"],
            ["Ready"] = ["Ready", "準備完了", "就緒", "就绪", "就緒", "준비됨"],
            ["ReadyToRun"] = ["Ready to run", "実行準備完了", "可開始執行", "可开始运行", "可開始執行", "실행 준비 완료"],
            ["ReadyDescription"] = ["Select a scenario and start a run to see each governance stage.", "シナリオを選択して実行すると、各ガバナンス段階を確認できます。", "選擇情境並開始執行，即可查看各治理階段。", "选择场景并开始运行，即可查看各治理阶段。", "選擇情境並開始執行，即可查看各管治階段。", "시나리오를 선택하고 실행하여 각 거버넌스 단계를 확인하세요."],
            ["FourStages"] = ["Four execution stages", "4 つの実行段階", "四個執行階段", "四个执行阶段", "四個執行階段", "4개 실행 단계"],
            ["Stage"] = ["Stage {0}", "ステージ {0}", "階段 {0}", "阶段 {0}", "階段 {0}", "단계 {0}"],
            ["GovernanceRejected"] = ["Governance rejected this request", "ガバナンスがこのリクエストを拒否しました", "治理機制拒絕此要求", "治理机制拒绝此请求", "管治機制拒絕此要求", "거버넌스가 이 요청을 거부했습니다"],
            ["DecisionReason"] = ["Decision reason", "判断理由", "決策原因", "决策原因", "決策原因", "결정 이유"],
            ["OutputDetail"] = ["Output / detail", "出力 / 詳細", "輸出 / 詳細資料", "输出 / 详细信息", "輸出 / 詳細資料", "출력 / 세부 정보"],
            ["Pending"] = ["Pending", "待機", "等待中", "等待中", "等候中", "대기"],
            ["Active"] = ["Active", "処理中", "處理中", "处理中", "處理中", "처리 중"],
            ["Succeeded"] = ["Succeeded", "成功", "成功", "成功", "成功", "성공"],
            ["Skipped"] = ["Skipped", "スキップ", "已略過", "已跳过", "已略過", "건너뜀"],
            ["Unknown"] = ["Unknown", "不明", "未知", "未知", "未知", "알 수 없음"],
            ["WaitingStage"] = ["Waiting for this stage.", "この段階を待機しています。", "正在等待此階段。", "正在等待此阶段。", "正在等候此階段。", "이 단계를 기다리는 중입니다."],
            ["ActiveStage"] = ["This stage is in progress.", "この段階を処理しています。", "此階段正在處理。", "此阶段正在处理。", "此階段正在處理。", "이 단계가 진행 중입니다."],
            ["CompletedStage"] = ["Completed successfully.", "正常に完了しました。", "已成功完成。", "已成功完成。", "已成功完成。", "성공적으로 완료했습니다."],
            ["BlockedStage"] = ["Blocked by governance.", "ガバナンスによりブロックされました。", "已被治理機制封鎖。", "已被治理机制阻止。", "已被管治機制封鎖。", "거버넌스에 의해 차단되었습니다."],
            ["SkippedStage"] = ["Not executed because an earlier stage stopped the flow.", "前の段階で停止したため実行されませんでした。", "因較早階段停止流程而未執行。", "因较早阶段停止流程而未执行。", "因較早階段停止流程而未執行。", "이전 단계에서 흐름이 중지되어 실행되지 않았습니다."],
            ["FailedStage"] = ["This stage did not complete.", "この段階は完了しませんでした。", "此階段未完成。", "此阶段未完成。", "此階段未完成。", "이 단계가 완료되지 않았습니다."],
            ["DecisionPlaceholder"] = ["The governance decision will appear here.", "ガバナンスの判断がここに表示されます。", "治理決策將顯示於此。", "治理决策将显示在此处。", "管治決策將顯示於此。", "거버넌스 결정이 여기에 표시됩니다."],
            ["NoDecision"] = ["No decision reason was provided.", "判断理由は提供されませんでした。", "未提供決策原因。", "未提供决策原因。", "未提供決策原因。", "결정 이유가 제공되지 않았습니다."],
            ["NoToolOutput"] = ["Blocked by governance. No tool output was produced.", "ガバナンスによりブロックされ、ツール出力は生成されませんでした。", "已被治理機制封鎖，未產生工具輸出。", "已被治理机制阻止，未生成工具输出。", "已被管治機制封鎖，未產生工具輸出。", "거버넌스에 의해 차단되어 도구 출력이 생성되지 않았습니다."],
            ["OutputPlaceholder"] = ["Run output and execution details will appear here.", "実行結果と詳細がここに表示されます。", "執行輸出與詳細資料將顯示於此。", "运行输出与详细信息将显示在此处。", "執行輸出與詳細資料將顯示於此。", "실행 출력과 세부 정보가 여기에 표시됩니다."],
            ["NoOutputAvailable"] = ["No output is available yet.", "利用可能な出力はまだありません。", "尚無可用輸出。", "尚无可用输出。", "尚無可用輸出。", "아직 사용 가능한 출력이 없습니다."],
            ["WeatherOutput"] = ["Seattle: 18°C, clear skies (simulated)", "シアトル: 18°C、快晴（シミュレーション）", "西雅圖：18°C，晴朗（模擬）", "西雅图：18°C，晴朗（模拟）", "西雅圖：18°C，天晴（模擬）", "시애틀: 18°C, 맑음(시뮬레이션)"],
            ["TimeOutput"] = ["2026-01-15T12:00:00Z (simulated)", "2026-01-15T12:00:00Z（シミュレーション）", "2026-01-15T12:00:00Z（模擬）", "2026-01-15T12:00:00Z（模拟）", "2026-01-15T12:00:00Z（模擬）", "2026-01-15T12:00:00Z(시뮬레이션)"],
            ["LocationOutput"] = ["Contoso Campus, Building 1 (simulated)", "Contoso キャンパス 1 号館（シミュレーション）", "Contoso 園區 1 號大樓（模擬）", "Contoso 园区 1 号楼（模拟）", "Contoso 園區 1 號大樓（模擬）", "Contoso 캠퍼스 1동(시뮬레이션)"],
            ["PersistedAudit"] = ["Persisted audit records", "Blob に保存された監査レコード", "儲存於 Blob 的稽核記錄", "存储在 Blob 中的审计日志", "儲存於 Blob 的審計記錄", "Blob에 저장된 감사 레코드"],
            ["RecordCount"] = ["{0} records", "{0} 件", "{0} 筆", "{0} 条", "{0} 項", "{0}개"],
            ["CurrentAuditSession"] = ["Current audit session", "現在の監査セッション", "目前稽核工作階段", "当前审计会话", "目前審計工作階段", "현재 감사 세션"],
            ["Restore"] = ["Reload from Blob", "Blob から再読込", "從 Blob 重新載入", "从 Blob 重新加载", "從 Blob 重新載入", "Blob에서 다시 불러오기"],
            ["Restoring"] = ["Restoring…", "復元中…", "還原中…", "正在恢复…", "還原中…", "복원 중…"],
            ["NoPersistedEvents"] = ["No persisted events", "永続化済みイベントはありません", "沒有已保存的事件", "没有已持久化的事件", "沒有已保存的事件", "저장된 이벤트가 없습니다"],
            ["PersistedOnly"] = ["Only records successfully appended to Blob are shown.", "Blob Append に成功したレコードだけが表示されます。", "只顯示成功附加至 Blob 的記錄。", "仅显示成功追加到 Blob 的记录。", "只顯示成功附加至 Blob 的記錄。", "Blob에 성공적으로 추가된 레코드만 표시됩니다."],
            ["Event"] = ["Event", "イベント", "事件", "事件", "事件", "이벤트"],
            ["Policy"] = ["Policy", "ポリシー", "原則", "策略", "原則", "정책"],
            ["ShowJson"] = ["Show JSON details", "JSON 詳細を表示", "顯示 JSON 詳細資料", "显示 JSON 详细信息", "顯示 JSON 詳細資料", "JSON 세부 정보 표시"],
            ["TelemetryConfigured"] = ["Azure Monitor telemetry is configured.", "Azure Monitor テレメトリは構成済みです。", "Azure Monitor 遙測已設定。", "Azure Monitor 遥测已配置。", "Azure Monitor 遙測已設定。", "Azure Monitor 텔레메트리가 구성되었습니다."],
            ["TelemetryInvalid"] = ["Azure Monitor telemetry is disabled because the connection string is invalid.", "接続文字列が無効なため Azure Monitor テレメトリは無効です。", "連線字串無效，因此 Azure Monitor 遙測已停用。", "连接字符串无效，因此 Azure Monitor 遥测已禁用。", "連線字串無效，因此 Azure Monitor 遙測已停用。", "연결 문자열이 잘못되어 Azure Monitor 텔레메트리가 비활성화되었습니다."],
            ["TelemetryDisabled"] = ["Azure Monitor telemetry is disabled because no connection string is configured.", "接続文字列が未設定のため Azure Monitor テレメトリは無効です。", "未設定連線字串，因此 Azure Monitor 遙測已停用。", "未配置连接字符串，因此 Azure Monitor 遥测已禁用。", "未設定連線字串，因此 Azure Monitor 遙測已停用。", "연결 문자열이 구성되지 않아 Azure Monitor 텔레메트리가 비활성화되었습니다."],
            ["DecisionAllowlist"] = ["The request matched an allowlist rule.", "リクエストは許可リストのルールに一致しました。", "要求符合允許清單規則。", "请求匹配允许列表规则。", "要求符合允許清單規則。", "요청이 허용 목록 규칙과 일치했습니다."],
            ["DecisionExplicitDeny"] = ["The request matched an explicit deny rule.", "リクエストは明示的な拒否ルールに一致しました。", "要求符合明確拒絕規則。", "请求匹配显式拒绝规则。", "要求符合明確拒絕規則。", "요청이 명시적 거부 규칙과 일치했습니다."],
            ["DecisionDefaultDeny"] = ["No allow rule matched, so default deny was applied.", "許可ルールに一致しなかったため、Default deny が適用されました。", "沒有符合的允許規則，因此套用 Default deny。", "没有匹配的允许规则，因此应用 Default deny。", "沒有符合的允許規則，因此套用 Default deny。", "일치하는 허용 규칙이 없어 Default deny가 적용되었습니다."],
            ["DecisionPromptInjection"] = ["Prompt injection was detected before tool execution.", "ツール実行前にプロンプトインジェクションが検出されました。", "在工具執行前偵測到提示詞注入。", "在工具执行前检测到提示词注入。", "在工具執行前偵測到提示詞注入。", "도구 실행 전에 프롬프트 인젝션이 탐지되었습니다."],
            ["Request"] = ["Request", "リクエスト", "要求", "请求", "要求", "요청"],
            ["GovernanceGate"] = ["Governance Gate", "ガバナンスゲート", "治理閘道", "治理网关", "管治閘道", "거버넌스 게이트"],
            ["ToolExecution"] = ["Tool Execution", "ツール実行", "工具執行", "工具执行", "工具執行", "도구 실행"],
            ["StorageHealthyMessage"] = ["Audit Blob access is available.", "監査 Blob へアクセスできます。", "可存取稽核 Blob。", "可以访问审计 Blob。", "可存取審計 Blob。", "감사 Blob에 액세스할 수 있습니다."],
            ["StorageFailedMessage"] = ["Blob Storage is unavailable.", "Blob Storage を利用できません。", "Blob Storage 無法使用。", "Blob Storage 不可用。", "Blob Storage 無法使用。", "Blob Storage를 사용할 수 없습니다."],
            ["StorageUnverifiedMessage"] = ["Blob Storage connectivity has not been verified.", "Blob Storage への接続はまだ確認されていません。", "尚未確認 Blob Storage 連線。", "尚未验证 Blob Storage 连接。", "尚未確認 Blob Storage 連線。", "Blob Storage 연결이 아직 확인되지 않았습니다."],
            ["PolicyCheck"] = ["Policy check", "ポリシーチェック", "原則檢查", "策略检查", "原則檢查", "정책 검사"],
            ["PolicyViolation"] = ["Policy violation", "ポリシー違反", "違反原則", "策略违规", "違反原則", "정책 위반"],
            ["ToolCallBlocked"] = ["Tool call blocked", "ツール呼び出し拒否", "工具呼叫已封鎖", "工具调用已阻止", "工具呼叫已封鎖", "도구 호출 차단"],
            ["TrustFailed"] = ["Trust verification failed", "信頼検証失敗", "信任驗證失敗", "信任验证失败", "信任驗證失敗", "신뢰 검증 실패"],
            ["TrustVerified"] = ["Trust verification succeeded", "信頼検証成功", "信任驗證成功", "信任验证成功", "信任驗證成功", "신뢰 검증 성공"],
            ["AppliedGatePath"] = ["Applied gate and C# code path", "適用ゲートと C# コードパス", "套用的閘道與 C# 程式碼路徑", "应用的网关与 C# 代码路径", "套用的閘道及 C# 程式碼路徑", "적용된 게이트 및 C# 코드 경로"],
            ["TargetTool"] = ["Target tool", "対象ツール", "目標工具", "目标工具", "目標工具", "대상 도구"],
            ["MatchedRule"] = ["Matched rule", "一致ルール", "符合的規則", "匹配规则", "符合的規則", "일치한 규칙"],
            ["AfterRun"] = ["Determined after the run", "実行後に確定", "執行後確定", "运行后确定", "執行後確定", "실행 후 결정"],
            ["BuiltInDetector"] = ["Built-in prompt-injection detector", "組み込み prompt-injection detector", "內建提示詞注入偵測器", "内置提示词注入检测器", "內建提示詞注入偵測器", "기본 제공 프롬프트 인젝션 탐지기"],
            ["ConditionAfterRun"] = ["The matched condition appears after the run.", "実行後に合致条件を表示します。", "執行後顯示符合條件。", "运行后显示匹配条件。", "執行後顯示符合條件。", "실행 후 일치 조건이 표시됩니다."],
            ["CodePathStatus"] = ["Governance code path status", "ガバナンスコードパスの状態", "治理程式碼路徑狀態", "治理代码路径状态", "管治程式碼路徑狀態", "거버넌스 코드 경로 상태"],
            ["SourceExcerpt"] = ["C# source excerpt for {0}", "{0} の C# ソースコード抜粋", "{0} 的 C# 原始碼摘錄", "{0} 的 C# 源代码摘录", "{0} 的 C# 原始碼摘錄", "{0}의 C# 소스 코드 발췌"],
            ["GateDefinition"] = ["Governance Gate definition file", "Governance Gate 定義ファイル", "Governance Gate 定義檔", "Governance Gate 定义文件", "Governance Gate 定義檔", "Governance Gate 정의 파일"],
            ["AppliedResult"] = ["Applied result", "適用結果", "套用結果", "应用结果", "套用結果", "적용 결과"],
            ["MatchedCondition"] = ["Matched condition", "合致条件", "符合條件", "匹配条件", "符合條件", "일치 조건"],
            ["YamlDefinition"] = ["Governance Gate YAML definition", "Governance Gate の YAML 定義", "Governance Gate YAML 定義", "Governance Gate YAML 定义", "Governance Gate YAML 定義", "Governance Gate YAML 정의"],
            ["Evaluating"] = ["Evaluating", "評価中", "評估中", "评估中", "評估中", "평가 중"],
            ["DeniedPath"] = ["Denied path", "拒否パス", "拒絕路徑", "拒绝路径", "拒絕路徑", "거부 경로"],
            ["NotTaken"] = ["Not taken", "非通過", "未採用", "未采用", "未採用", "통과하지 않음"],
            ["NotEvaluated"] = ["Not evaluated", "未評価", "未評估", "未评估", "未評估", "평가되지 않음"],
            ["Passed"] = ["Passed", "通過", "通過", "通过", "通過", "통과"],
            ["NotPassed"] = ["Did not pass", "通過不可", "未通過", "未通过", "未通過", "통과하지 못함"],
            ["EvaluationFailed"] = ["Evaluation failed", "評価失敗", "評估失敗", "评估失败", "評估失敗", "평가 실패"],
            ["AllowlistRule"] = ["Allowlist rule", "Allowlist ルール", "允許清單規則", "允许列表规则", "允許清單規則", "허용 목록 규칙"],
            ["ExplicitDenyRule"] = ["Explicit deny rule", "明示的 deny ルール", "明確拒絕規則", "显式拒绝规则", "明確拒絕規則", "명시적 거부 규칙"],
            ["PromptInjectionDetection"] = ["Prompt injection detection", "Prompt injection 検出", "提示詞注入偵測", "提示词注入检测", "提示詞注入偵測", "프롬프트 인젝션 탐지"],
            ["UnknownGate"] = ["Unknown gate", "不明なゲート", "未知閘道", "未知网关", "未知閘道", "알 수 없는 게이트"],
            ["GateAllowedSummary"] = ["{0} allowed {1}; the deny path was not taken.", "{0}が {1} を許可したため、deny ブロックパスは通過していません。", "{0} 已允許 {1}，因此未採用拒絕路徑。", "{0} 已允许 {1}，因此未采用拒绝路径。", "{0} 已允許 {1}，因此未採用拒絕路徑。", "{0}가 {1}을(를) 허용하여 거부 경로를 통과하지 않았습니다."],
            ["GateDeniedSummary"] = ["{0} denied {1} and blocked tool execution.", "{0}が {1} を拒否し、ツール実行をブロックしました。", "{0} 拒絕 {1} 並封鎖工具執行。", "{0} 拒绝 {1} 并阻止工具执行。", "{0} 拒絕 {1} 並封鎖工具執行。", "{0}가 {1}을(를) 거부하고 도구 실행을 차단했습니다."],
            ["GateEvaluatingSummary"] = ["{0} is evaluating {1}.", "{0}で {1} を評価しています。", "{0} 正在評估 {1}。", "{0} 正在评估 {1}。", "{0} 正在評估 {1}。", "{0}에서 {1}을(를) 평가하고 있습니다."],
            ["GateEvaluationFailed"] = ["Governance Gate evaluation could not be completed.", "Governance Gate の評価を完了できませんでした。", "無法完成 Governance Gate 評估。", "无法完成 Governance Gate 评估。", "無法完成 Governance Gate 評估。", "Governance Gate 평가를 완료할 수 없습니다."],
            ["GatePendingSummary"] = ["{0} will be evaluated by {1} before execution.", "{0} の実行前に {1}を適用する予定です。", "執行 {0} 前將由 {1} 評估。", "执行 {0} 前将由 {1} 评估。", "執行 {0} 前將由 {1} 評估。", "{0} 실행 전에 {1}에서 평가합니다."],
            ["PolicyAllowedSummary"] = ["{0} passed the Governance Gate.", "{0} は Governance Gate を通過しました。", "{0} 已通過 Governance Gate。", "{0} 已通过 Governance Gate。", "{0} 已通過 Governance Gate。", "{0}이(가) Governance Gate를 통과했습니다."],
            ["PolicyDeniedSummary"] = ["{0} did not pass the Governance Gate.", "{0} は Governance Gate を通過できませんでした。", "{0} 未通過 Governance Gate。", "{0} 未通过 Governance Gate。", "{0} 未通過 Governance Gate。", "{0}이(가) Governance Gate를 통과하지 못했습니다."],
            ["PolicyEvaluatingSummary"] = ["{0} is being checked against the definition file.", "{0} を定義ファイルの条件と照合しています。", "正在依定義檔條件檢查 {0}。", "正在根据定义文件条件检查 {0}。", "正在依定義檔條件檢查 {0}。", "{0}을(를) 정의 파일 조건과 비교하고 있습니다."],
            ["PolicyPendingSummary"] = ["{0} has not yet been evaluated by the Governance Gate.", "{0} はまだ Governance Gate で評価されていません。", "{0} 尚未由 Governance Gate 評估。", "{0} 尚未由 Governance Gate 评估。", "{0} 尚未由 Governance Gate 評估。", "{0}은(는) 아직 Governance Gate에서 평가되지 않았습니다."],
            ["EvaluationRequest"] = ["Evaluation request", "評価要求", "評估要求", "评估请求", "評估要求", "평가 요청"],
            ["ToolkitGate"] = ["Toolkit gate", "Toolkit ゲート", "Toolkit 閘道", "Toolkit 网关", "Toolkit 閘道", "Toolkit 게이트"],
            ["DenyBranch"] = ["Deny branch", "deny 分岐", "拒絕分支", "拒绝分支", "拒絕分支", "거부 분기"],
            ["RecordSkipped"] = ["Record execution as skipped", "実行を Skipped に記録", "將執行記錄為已略過", "将执行记录为已跳过", "將執行記錄為已略過", "실행을 건너뜀으로 기록"],
            ["EarlyReturn"] = ["Early return", "早期 return", "提前返回", "提前返回", "提前返回", "조기 반환"],
            ["EvaluationRequestDescription"] = ["Passes the selected tool and arguments to one governance boundary.", "選択されたツール名と引数を単一のガバナンス境界へ渡します。", "將所選工具與引數傳至單一治理邊界。", "将所选工具与参数传递到单一治理边界。", "將所選工具及引數傳至單一管治邊界。", "선택한 도구와 인수를 단일 거버넌스 경계로 전달합니다."],
            ["ToolkitGateDescription"] = ["Evaluates policy and prompt injection before tool execution.", "ポリシーと prompt injection 検出をツール実行前に評価します。", "在工具執行前評估原則與提示詞注入。", "在工具执行前评估策略与提示词注入。", "在工具執行前評估原則及提示詞注入。", "도구 실행 전에 정책과 프롬프트 인젝션을 평가합니다."],
            ["DenyBranchDescription"] = ["Enters blocking logic only when Allowed is false.", "Allowed が false の場合だけブロック処理へ進みます。", "僅在 Allowed 為 false 時進入封鎖邏輯。", "仅在 Allowed 为 false 时进入阻止逻辑。", "只在 Allowed 為 false 時進入封鎖邏輯。", "Allowed가 false인 경우에만 차단 로직으로 이동합니다."],
            ["RecordSkippedDescription"] = ["Does not call the tool and explicitly records that it was not executed.", "ツールを呼び出さず、未実行であることを明示します。", "不呼叫工具，並明確記錄未執行。", "不调用工具，并明确记录未执行。", "不呼叫工具，並明確記錄未執行。", "도구를 호출하지 않고 실행되지 않았음을 명시합니다."],
            ["EarlyReturnDescription"] = ["Ends before reaching the allowed tool execution boundary.", "許可後のツール実行境界へ到達する前に処理を終了します。", "在到達允許的工具執行邊界前結束。", "在到达允许的工具执行边界前结束。", "在到達允許的工具執行邊界前結束。", "허용된 도구 실행 경계에 도달하기 전에 종료합니다."],
            ["GovernanceEvaluationSnippet"] = ["1. Governance evaluation", "1. ガバナンス評価", "1. 治理評估", "1. 治理评估", "1. 管治評估", "1. 거버넌스 평가"],
            ["DenyHandlingSnippet"] = ["2. Denial handling", "2. 拒否時のブロック処理", "2. 拒絕處理", "2. 拒绝处理", "2. 拒絕處理", "2. 거부 처리"],
            ["ErrorTitle"] = ["Error", "エラー", "錯誤", "错误", "錯誤", "오류"],
            ["ErrorMessage"] = ["An error occurred while processing your request.", "リクエストの処理中にエラーが発生しました。", "處理要求時發生錯誤。", "处理请求时发生错误。", "處理要求時發生錯誤。", "요청을 처리하는 동안 오류가 발생했습니다."],
            ["RequestId"] = ["Request ID", "リクエスト ID", "要求 ID", "请求 ID", "要求 ID", "요청 ID"]
            ,
            ["DevelopmentMode"] = ["Development mode", "開発モード", "開發模式", "开发模式", "開發模式", "개발 모드"],
            ["DevelopmentDescription"] = ["Using the Development environment displays more detailed error information.", "Development 環境に切り替えると、より詳しいエラー情報が表示されます。", "切換至 Development 環境會顯示更詳細的錯誤資訊。", "切换到 Development 环境会显示更详细的错误信息。", "切換至 Development 環境會顯示更詳細的錯誤資料。", "Development 환경으로 전환하면 더 자세한 오류 정보가 표시됩니다."],
            ["DevelopmentWarning"] = ["Do not enable Development for deployed applications because exception details may expose sensitive information. Use ASPNETCORE_ENVIRONMENT=Development only for local debugging, then restart the app.", "配置済みアプリケーションでは、例外の詳細から機密情報が公開される可能性があるため Development 環境を有効にしないでください。ローカルデバッグ時のみ ASPNETCORE_ENVIRONMENT=Development を設定し、アプリを再起動してください。", "已部署的應用程式不應啟用 Development，因為例外詳細資料可能洩漏敏感資訊。僅在本機偵錯時設定 ASPNETCORE_ENVIRONMENT=Development，然後重新啟動應用程式。", "已部署的应用不应启用 Development，因为异常详细信息可能泄露敏感信息。仅在本地调试时设置 ASPNETCORE_ENVIRONMENT=Development，然后重启应用。", "已部署的應用程式不應啟用 Development，因為例外詳細資料可能洩漏敏感資料。只在本機偵錯時設定 ASPNETCORE_ENVIRONMENT=Development，然後重新啟動應用程式。", "배포된 애플리케이션에서는 예외 세부 정보로 민감한 정보가 노출될 수 있으므로 Development 환경을 사용하지 마세요. 로컬 디버깅 시에만 ASPNETCORE_ENVIRONMENT=Development를 설정하고 앱을 다시 시작하세요."]
        };

    private string _culture;

    public UiLocalizer()
    {
        _culture = SupportedCultures.FirstOrDefault(
            item => string.Equals(
                item,
                CultureInfo.CurrentUICulture.Name,
                StringComparison.OrdinalIgnoreCase))
            ?? DefaultCulture;
    }

    public event Action? Changed;

    public string Culture => _culture;

    public string this[string key] =>
        Text.TryGetValue(key, out var values)
            ? values[CultureIndex(_culture)]
            : key;

    public string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.GetCultureInfo(_culture), this[key], arguments);

    public bool SetCulture(string? culture)
    {
        var normalized = SupportedCultures.FirstOrDefault(
            item => string.Equals(item, culture, StringComparison.OrdinalIgnoreCase));
        if (normalized is null || normalized == _culture)
        {
            return false;
        }

        _culture = normalized;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(normalized);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(normalized);
        Changed?.Invoke();
        return true;
    }

    private static int CultureIndex(string culture) =>
        culture switch
        {
            "en-US" => 0,
            "ja-JP" => 1,
            "zh-TW" => 2,
            "zh-CN" => 3,
            "zh-HK" => 4,
            "ko-KR" => 5,
            _ => 1
        };
}
