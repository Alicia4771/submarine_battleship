using System;
using System.Collections;
using System.Collections.Generic;
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
        "Button4による信号入力を管理するController")]
    private SignalInputController
        signalInputController;


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
    // 結果表示時間
    // ============================================================

    [Header("Mission Result")]

    [SerializeField, Tooltip(
        "成功・失敗状態を維持してから次の索敵へ戻るまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float resultStateDuration =
        DefaultResultStateDuration;


    // ============================================================
    // デバッグ
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "通信状態をConsoleへ表示する")]
    private bool debugLog =
        true;


    // ============================================================
    // 内部状態
    // ============================================================

    private MissionState currentState =
        MissionState.Searching;


    private EnemyShip activeEnemyShip;


    private readonly List<SignalSymbol>
        targetSignal =
            new();


    private Coroutine resetMissionCoroutine;


    // ============================================================
    // イベント
    // ============================================================

    public event Action<MissionState>
        MissionStateChanged;


    public event Action<bool>
        MissionEvaluated;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        if (
            signalInputController ==
            null
        )
        {
            signalInputController =
                FindFirstObjectByType<
                    SignalInputController
                >();
        }


        if (
            signalInputController !=
            null
        )
        {
            signalInputController
                .ClearInput();
        }


        SetState(
            MissionState.Searching
        );
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
    // 敵艦発見
    // ============================================================

    /// <summary>
    /// 敵艦から新しい通信ミッションを開始する。
    ///
    /// 既に別の通信ミッション中の場合はfalse。
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
        // 正解信号コピー
        // =========================

        targetSignal.Clear();


        for (
            int i = 0;
            i < signalPattern.Count;
            i++
        )
        {
            targetSignal.Add(
                signalPattern[i]
            );
        }


        // =========================
        // 前回入力を消去
        // =========================

        if (
            signalInputController !=
            null
        )
        {
            signalInputController
                .ClearInput();
        }


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
    // 潜望鏡格納判定
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


        if (
            signalInputController !=
            null
        )
        {
            signalInputController
                .ClearInput();
        }


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
    // 必要な信号数
    // ============================================================

    public int GetExpectedSignalLength()
    {
        return
            targetSignal.Count;
    }


    // ============================================================
    // プレイヤー信号受信
    // ============================================================

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


        if (playerSignal == null)
        {
            return;
        }


        SetState(
            MissionState.Evaluating
        );


        bool success =
            CompareSignals(
                targetSignal,
                playerSignal
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
    // 成功
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
    // 失敗
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
    // 次ミッションへ
    // ============================================================

    private void StartResetMission()
    {
        if (
            resetMissionCoroutine !=
            null
        )
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


    private IEnumerator
        ResetMissionAfterDelay()
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


    private void ResetCurrentMission()
    {
        activeEnemyShip =
            null;


        targetSignal.Clear();


        if (
            signalInputController !=
            null
        )
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
    /// 通信対象の敵艦が途中で破棄された場合に
    /// ミッションを安全に初期化する。
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


        if (
            resetMissionCoroutine !=
            null
        )
        {
            StopCoroutine(
                resetMissionCoroutine
            );

            resetMissionCoroutine =
                null;
        }


        ResetCurrentMission();
    }


    // ============================================================
    // 状態
    // ============================================================

    private void SetState(
        MissionState newState
    )
    {
        if (
            currentState ==
            newState
        )
        {
            return;
        }


        currentState =
            newState;


        MissionStateChanged?.Invoke(
            currentState
        );
    }


    public MissionState GetCurrentState()
    {
        return
            currentState;
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


        System.Text.StringBuilder
            builder =
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