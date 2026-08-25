using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CommunicationMissionManager : MonoBehaviour
{
    // ============================================================
    // ミッション状態
    // ============================================================

    public enum MissionState
    {
        Searching,
        Memorizing,
        WaitingForSubmerge,
        Inputting,
        Transmitting,
        Evaluating,
        Success,
        Failed
    }


    // ============================================================
    // 定数
    // ============================================================

    private const int DefaultSuccessScore = 100;
    private const int DefaultFailureScorePenalty = 50;

    private const float DefaultResultStateDuration = 1.5f;

    private const float MinimumNonNegativeValue = 0.0f;


    // ============================================================
    // Inspector設定
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "Button4による信号入力を管理するController。" +
        "未設定の場合はシーン内から自動検索する")]
    private SignalInputController
        signalInputController;


    [SerializeField, Tooltip(
        "司令部への送信に使用する通信マスト。" +
        "未設定の場合はシーン内から自動検索する")]
    private CommunicationMastController
        communicationMastController;


    // ============================================================
    // スコア
    // ============================================================

    [Header("Score")]

    [SerializeField, Tooltip(
        "通信成功時に加算するスコア")]
    [Min(0)]
    private int successScore =
        DefaultSuccessScore;


    [SerializeField, Tooltip(
        "通信失敗時に減算するスコア")]
    [Min(0)]
    private int failureScorePenalty =
        DefaultFailureScorePenalty;


    // ============================================================
    // 結果表示
    // ============================================================

    [Header("Mission Result")]

    [SerializeField, Tooltip(
        "成功・失敗判定後、" +
        "次の索敵状態へ戻るまでの待ち時間")]
    [Min(MinimumNonNegativeValue)]
    private float resultStateDuration =
        DefaultResultStateDuration;


    // ============================================================
    // デバッグ
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "通信ミッションの状態をConsoleへ表示する")]
    private bool debugLog = true;


    // ============================================================
    // 内部状態
    // ============================================================

    private MissionState currentState =
        MissionState.Searching;


    private EnemyShip activeEnemyShip;


    // 敵艦から傍受した正解信号
    private readonly List<SignalSymbol>
        targetSignal =
            new();


    // プレイヤーが入力し、
    // 通信マストから送信している信号
    private readonly List<SignalSymbol>
        pendingPlayerSignal =
            new();


    private Coroutine resetMissionCoroutine;


    // ============================================================
    // イベント
    // ============================================================

    /// <summary>
    /// ミッション状態が変化したときに発生する。
    ///
    /// GameManagerがSearching状態を検知して
    /// 次の敵艦を生成する処理などに使用できる。
    /// </summary>
    public event Action<MissionState>
        MissionStateChanged;


    /// <summary>
    /// 信号の正誤判定が完了した時に発生する。
    ///
    /// true  = 成功
    /// false = 失敗
    /// </summary>
    public event Action<bool>
        MissionEvaluated;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        SubscribeMastEvents();


        if (signalInputController != null)
        {
            signalInputController
                .ClearInput();
        }


        currentState =
            MissionState.Searching;
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        // =========================
        // 潜望鏡格納待ち
        // =========================

        if (
            currentState ==
            MissionState.WaitingForSubmerge
        )
        {
            CheckPeriscopeLowered();
        }
    }


    // ============================================================
    // OnDestroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeMastEvents();


        if (resetMissionCoroutine != null)
        {
            StopCoroutine(
                resetMissionCoroutine
            );

            resetMissionCoroutine =
                null;
        }
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (signalInputController == null)
        {
            signalInputController =
                FindFirstObjectByType<
                    SignalInputController
                >();
        }


        if (
            communicationMastController ==
            null
        )
        {
            communicationMastController =
                FindFirstObjectByType<
                    CommunicationMastController
                >();
        }


        if (
            signalInputController ==
            null
        )
        {
            Debug.LogError(
                "SignalInputControllerが見つかりません。"
            );
        }


        if (
            communicationMastController ==
            null
        )
        {
            Debug.LogWarning(
                "CommunicationMastControllerが見つかりません。" +
                "通信マスト演出なしで正誤判定を行います。"
            );
        }
    }


    // ============================================================
    // 通信マストイベント
    // ============================================================

    private void SubscribeMastEvents()
    {
        if (
            communicationMastController ==
            null
        )
        {
            return;
        }


        communicationMastController
            .TransmissionCompleted +=
                HandleTransmissionCompleted;
    }


    private void UnsubscribeMastEvents()
    {
        if (
            communicationMastController ==
            null
        )
        {
            return;
        }


        communicationMastController
            .TransmissionCompleted -=
                HandleTransmissionCompleted;
    }


    // ============================================================
    // 敵艦発見
    // ============================================================

    /// <summary>
    /// EnemyShipから通信ミッション開始を要求される。
    ///
    /// 既に別のミッション中ならfalse。
    /// </summary>
    public bool TryBeginMission(
        EnemyShip enemyShip,
        IReadOnlyList<SignalSymbol>
            signalPattern
    )
    {
        if (enemyShip == null)
        {
            return false;
        }


        if (
            signalPattern == null ||
            signalPattern.Count <= 0
        )
        {
            return false;
        }


        if (
            currentState !=
            MissionState.Searching
        )
        {
            return false;
        }


        // =========================
        // 対象敵艦
        // =========================

        activeEnemyShip =
            enemyShip;


        // =========================
        // 正解信号
        // =========================

        targetSignal.Clear();


        CopySignal(
            signalPattern,
            targetSignal
        );


        // =========================
        // 前回の入力
        // =========================

        pendingPlayerSignal.Clear();


        if (signalInputController != null)
        {
            signalInputController
                .ClearInput();
        }


        // =========================
        // 傍受状態
        // =========================

        SetState(
            MissionState.Memorizing
        );


        if (debugLog)
        {
            Debug.Log(
                "通信傍受開始: " +
                SignalToDebugString(
                    targetSignal
                )
            );
        }


        return true;
    }


    // ============================================================
    // 敵信号終了
    // ============================================================

    public void NotifyEnemySignalFinished(
        EnemyShip enemyShip
    )
    {
        if (
            enemyShip == null ||
            enemyShip != activeEnemyShip
        )
        {
            return;
        }


        if (
            currentState !=
            MissionState.Memorizing
        )
        {
            return;
        }


        SetState(
            MissionState.WaitingForSubmerge
        );


        if (debugLog)
        {
            Debug.Log(
                "敵信号終了。" +
                "潜望鏡を完全に下げてください。"
            );
        }
    }


    // ============================================================
    // 潜望鏡格納確認
    // ============================================================

    private void CheckPeriscopeLowered()
    {
        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            return;
        }


        BeginSignalInput();
    }


    // ============================================================
    // 信号入力開始
    // ============================================================

    private void BeginSignalInput()
    {
        if (
            currentState !=
            MissionState.WaitingForSubmerge
        )
        {
            return;
        }


        if (signalInputController != null)
        {
            signalInputController
                .ClearInput();
        }


        pendingPlayerSignal.Clear();


        SetState(
            MissionState.Inputting
        );


        if (debugLog)
        {
            Debug.Log(
                "信号入力開始。" +
                "Button4で信号を再現してください。"
            );
        }
    }


    // ============================================================
    // Button4入力可否
    // ============================================================

    public bool CanAcceptSignalInput()
    {
        return
            currentState ==
            MissionState.Inputting;
    }


    // ============================================================
    // 必要な記号数
    // ============================================================

    public int GetExpectedSignalLength()
    {
        return
            targetSignal.Count;
    }


    // ============================================================
    // プレイヤー信号提出
    // ============================================================

    /// <summary>
    /// SignalInputControllerから呼び出される。
    ///
    /// 第3段階ではここで正誤判定せず、
    /// 通信マストによる送信を開始する。
    /// </summary>
    public void SubmitPlayerSignal(
        IReadOnlyList<SignalSymbol>
            playerSignal
    )
    {
        if (
            currentState !=
            MissionState.Inputting
        )
        {
            return;
        }


        if (
            playerSignal == null ||
            playerSignal.Count <= 0
        )
        {
            return;
        }


        // =========================
        // 入力信号を保存
        // =========================

        pendingPlayerSignal.Clear();


        CopySignal(
            playerSignal,
            pendingPlayerSignal
        );


        // =========================
        // 送信状態
        // =========================

        SetState(
            MissionState.Transmitting
        );


        if (debugLog)
        {
            Debug.Log(
                "信号入力完了。" +
                "司令部への送信を開始します。"
            );
        }


        StartCommunicationTransmission();
    }


    // ============================================================
    // 通信マスト送信開始
    // ============================================================

    private void StartCommunicationTransmission()
    {
        // =========================
        // マスト未設定時
        // =========================
        //
        // 開発途中でもゲーム進行が完全停止しないよう
        // 通信マストなしの場合は直接判定する。
        // =========================

        if (
            communicationMastController ==
            null
        )
        {
            Debug.LogWarning(
                "CommunicationMastControllerが存在しないため、" +
                "通信マスト処理を省略します。"
            );


            EvaluatePendingSignal();

            return;
        }


        bool started =
            communicationMastController
                .TryStartTransmission();


        // =========================
        // 開始失敗
        // =========================

        if (!started)
        {
            Debug.LogWarning(
                "通信マストを開始できなかったため、" +
                "通信マスト処理を省略して判定します。"
            );


            EvaluatePendingSignal();
        }
    }


    // ============================================================
    // 通信マスト終了通知
    // ============================================================

    private void HandleTransmissionCompleted()
    {
        // ミッション終了などで
        // 状態が既に変わっていた場合は無視
        if (
            currentState !=
            MissionState.Transmitting
        )
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "司令部への送信が完了しました。" +
                "信号を照合します。"
            );
        }


        EvaluatePendingSignal();
    }


    // ============================================================
    // 正誤判定
    // ============================================================

    private void EvaluatePendingSignal()
    {
        if (
            currentState !=
            MissionState.Transmitting
        )
        {
            return;
        }


        SetState(
            MissionState.Evaluating
        );


        bool success =
            CompareSignals(
                targetSignal,
                pendingPlayerSignal
            );


        if (success)
        {
            HandleMissionSuccess();
        }
        else
        {
            HandleMissionFailure();
        }
    }


    // ============================================================
    // 信号比較
    // ============================================================

    private bool CompareSignals(
        IReadOnlyList<SignalSymbol>
            expected,
        IReadOnlyList<SignalSymbol>
            actual
    )
    {
        if (
            expected == null ||
            actual == null
        )
        {
            return false;
        }


        if (
            expected.Count !=
            actual.Count
        )
        {
            return false;
        }


        for (
            int i = 0;
            i < expected.Count;
            i++
        )
        {
            if (
                expected[i] !=
                actual[i]
            )
            {
                return false;
            }
        }


        return true;
    }


    // ============================================================
    // 通信成功
    // ============================================================

    private void HandleMissionSuccess()
    {
        DataManager.AddScore(
            successScore
        );


        SetState(
            MissionState.Success
        );


        if (debugLog)
        {
            Debug.Log(
                "通信成功！ +" +
                successScore +
                "点"
            );
        }


        MissionEvaluated?.Invoke(
            true
        );


        StartResetMission();
    }


    // ============================================================
    // 通信失敗
    // ============================================================

    private void HandleMissionFailure()
    {
        DataManager.AddScore(
            -failureScorePenalty
        );


        SetState(
            MissionState.Failed
        );


        if (debugLog)
        {
            Debug.Log(
                "通信失敗。 -" +
                failureScorePenalty +
                "点"
            );
        }


        MissionEvaluated?.Invoke(
            false
        );


        StartResetMission();
    }


    // ============================================================
    // 次のミッション
    // ============================================================

    private void StartResetMission()
    {
        if (resetMissionCoroutine != null)
        {
            StopCoroutine(
                resetMissionCoroutine
            );
        }


        resetMissionCoroutine =
            StartCoroutine(
                ResetMissionAfterDelay()
            );
    }


    private IEnumerator ResetMissionAfterDelay()
    {
        if (
            resultStateDuration >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    resultStateDuration
                );
        }


        ResetCurrentMission();


        resetMissionCoroutine =
            null;
    }


    // ============================================================
    // ミッション初期化
    // ============================================================

    private void ResetCurrentMission()
    {
        activeEnemyShip =
            null;


        targetSignal.Clear();


        pendingPlayerSignal.Clear();


        if (signalInputController != null)
        {
            signalInputController
                .ClearInput();
        }


        SetState(
            MissionState.Searching
        );


        if (debugLog)
        {
            Debug.Log(
                "次の通信対象を探索してください。"
            );
        }
    }


    // ============================================================
    // 敵艦破棄
    // ============================================================

    /// <summary>
    /// 通信対象の敵艦が途中でDestroyされた場合に、
    /// ミッションを安全に初期状態へ戻す。
    /// </summary>
    public void NotifyEnemyDestroyed(
        EnemyShip enemyShip
    )
    {
        if (
            enemyShip == null ||
            enemyShip != activeEnemyShip
        )
        {
            return;
        }


        if (resetMissionCoroutine != null)
        {
            StopCoroutine(
                resetMissionCoroutine
            );

            resetMissionCoroutine =
                null;
        }


        if (
            communicationMastController !=
            null &&
            communicationMastController
                .GetIsMastExposed()
        )
        {
            communicationMastController
                .CancelTransmission();
        }


        ResetCurrentMission();
    }


    // ============================================================
    // 状態変更
    // ============================================================

    private void SetState(
        MissionState newState
    )
    {
        if (currentState == newState)
        {
            return;
        }


        currentState =
            newState;


        MissionStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // 状態取得
    // ============================================================

    public MissionState GetCurrentState()
    {
        return
            currentState;
    }


    public EnemyShip GetActiveEnemyShip()
    {
        return
            activeEnemyShip;
    }


    // ============================================================
    // 信号コピー
    // ============================================================

    private void CopySignal(
        IReadOnlyList<SignalSymbol>
            source,
        List<SignalSymbol>
            destination
    )
    {
        if (
            source == null ||
            destination == null
        )
        {
            return;
        }


        for (
            int i = 0;
            i < source.Count;
            i++
        )
        {
            destination.Add(
                source[i]
            );
        }
    }


    // ============================================================
    // デバッグ文字列
    // ============================================================

    private string SignalToDebugString(
        IReadOnlyList<SignalSymbol>
            signal
    )
    {
        if (
            signal == null ||
            signal.Count <= 0
        )
        {
            return
                "(empty)";
        }


        StringBuilder builder =
            new();


        for (
            int i = 0;
            i < signal.Count;
            i++
        )
        {
            switch (signal[i])
            {
                case SignalSymbol.Short:

                    builder.Append(
                        "・"
                    );

                    break;


                case SignalSymbol.Long:

                    builder.Append(
                        "―"
                    );

                    break;
            }


            if (
                i <
                signal.Count - 1
            )
            {
                builder.Append(
                    " "
                );
            }
        }


        return
            builder.ToString();
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        successScore =
            Mathf.Max(
                0,
                successScore
            );


        failureScorePenalty =
            Mathf.Max(
                0,
                failureScorePenalty
            );


        resultStateDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                resultStateDuration
            );
    }
}