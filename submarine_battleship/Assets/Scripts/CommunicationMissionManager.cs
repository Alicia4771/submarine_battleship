using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CommunicationMissionManager : MonoBehaviour
{
    // ============================================================
    // Mission State
    // ============================================================

    public enum MissionState
    {
        Searching = 0,

        // 敵船の信号を見て覚えている状態
        Memorizing = 1,

        // 敵船の信号は終了したが、
        // まだ潜望鏡が完全格納されていない状態
        WaitingForSubmerge = 2,

        // Button4による入力受付
        Inputting = 3,

        // 通信マストによる送信
        Transmitting = 4,

        // 正誤判定
        Evaluating = 5,

        // 成功
        Success = 6,

        // 失敗
        Failed = 7
    }


    // ============================================================
    // 定数
    // ============================================================

    private const int DefaultSuccessScore =
        100;

    private const int DefaultFailureScore =
        -50;

    private const float DefaultEvaluationDelay =
        0.25f;

    private const float DefaultResultStateDuration =
        0.10f;

    private const float DefaultFallbackTransmissionDuration =
        2.0f;

    private const float MinimumNonNegativeValue =
        0.0f;


    // ============================================================
    // Event
    // ============================================================

    /// <summary>
    /// MissionStateが変更されたときに発生する。
    /// </summary>
    public event Action<MissionState>
        MissionStateChanged;


    /// <summary>
    /// 通信の正誤判定が完了したときに発生する。
    /// true = 成功
    /// false = 失敗
    /// </summary>
    public event Action<bool>
        MissionEvaluated;


    // ============================================================
    // Mast
    // ============================================================

    [Header("Communication Mast")]

    [SerializeField, Tooltip(
        "通信マストを制御するCommunicationMastController。" +
        "未設定の場合は自動検索する")]
    private CommunicationMastController
        communicationMastController;


    // ============================================================
    // Score
    // ============================================================

    [Header("Score")]

    [SerializeField, Tooltip(
        "通信成功時に加算するスコア")]
    private int successScore =
        DefaultSuccessScore;


    [SerializeField, Tooltip(
        "通信失敗時に加算するスコア。" +
        "減点する場合は負の値にする")]
    private int failureScore =
        DefaultFailureScore;


    // ============================================================
    // Timing
    // ============================================================

    [Header("Timing")]

    [SerializeField, Tooltip(
        "通信マスト格納後、正誤判定結果を出すまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float evaluationDelay =
        DefaultEvaluationDelay;


    [SerializeField, Tooltip(
        "Success / Failed状態を維持してから" +
        "Searchingへ戻るまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float resultStateDuration =
        DefaultResultStateDuration;


    [SerializeField, Tooltip(
        "CommunicationMastControllerが存在しない場合に" +
        "使用する仮の送信時間")]
    [Min(MinimumNonNegativeValue)]
    private float fallbackTransmissionDuration =
        DefaultFallbackTransmissionDuration;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        true;


    // ============================================================
    // 内部状態
    // ============================================================

    [SerializeField]
    private MissionState currentState =
        MissionState.Searching;


    private EnemyShip
        activeEnemy;


    private readonly List<SignalSymbol>
        expectedSignal =
            new List<SignalSymbol>();


    private readonly List<SignalSymbol>
        submittedSignal =
            new List<SignalSymbol>();


    private bool enemySignalFinished =
        false;


    private bool lastMissionWasSuccessful =
        false;


    private Coroutine evaluationCoroutine;

    private Coroutine fallbackTransmissionCoroutine;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        currentState =
            MissionState.Searching;
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        UpdatePeriscopeLoweredState();
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
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
            communicationMastController ==
                null
            &&
            debugLog
        )
        {
            Debug.LogWarning(
                "CommunicationMissionManager: " +
                "CommunicationMastControllerが見つかりません。"
            );
        }
    }


    // ============================================================
    // 潜望鏡状態監視
    // ============================================================

    private void UpdatePeriscopeLoweredState()
    {
        // ========================================================
        // 今回の重要な変更部分
        // ========================================================
        //
        // 敵船の信号が最後まで終わっているかどうかに関係なく、
        // 潜望鏡が完全格納されたら即座にInputtingへ移る。
        //
        // Memorizing
        //      ↓
        // 潜望鏡完全格納
        //      ↓
        // Inputting
        //
        // WaitingForSubmerge
        //      ↓
        // 潜望鏡完全格納
        //      ↓
        // Inputting
        // ========================================================

        if (
            currentState !=
                MissionState.Memorizing
            &&
            currentState !=
                MissionState.WaitingForSubmerge
        )
        {
            return;
        }


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
    // Mission開始
    // ============================================================

    public bool TryBeginMission(
        EnemyShip enemyShip,
        IReadOnlyList<SignalSymbol> signalPattern
    )
    {
        // 既に別ミッション中
        if (
            currentState !=
            MissionState.Searching
        )
        {
            return false;
        }


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


        activeEnemy =
            enemyShip;


        expectedSignal.Clear();


        for (
            int signalIndex = 0;
            signalIndex < signalPattern.Count;
            signalIndex++
        )
        {
            expectedSignal.Add(
                signalPattern[
                    signalIndex
                ]
            );
        }


        submittedSignal.Clear();


        enemySignalFinished =
            false;


        lastMissionWasSuccessful =
            false;


        SetMissionState(
            MissionState.Memorizing
        );


        if (debugLog)
        {
            Debug.Log(
                "通信傍受開始: " +
                ConvertSignalToString(
                    expectedSignal
                )
            );
        }


        return true;
    }


    // ============================================================
    // 敵船信号終了通知
    // ============================================================

    public void NotifyEnemySignalFinished(
        EnemyShip enemyShip
    )
    {
        // 別の敵船なら無視
        if (
            enemyShip == null ||
            enemyShip != activeEnemy
        )
        {
            return;
        }


        enemySignalFinished =
            true;


        // ========================================================
        // 既に入力以降へ進んでいる場合
        // ========================================================
        //
        // 今回、信号途中で潜望鏡を下げることができるため、
        // Inputtingへ移った後にこの通知が届くことがある。
        //
        // その場合は状態を巻き戻してはいけない。
        // ========================================================

        if (
            currentState !=
            MissionState.Memorizing
        )
        {
            return;
        }


        // 潜望鏡が既に完全格納されているなら
        // 即座に入力開始
        if (
            DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            BeginSignalInput();

            return;
        }


        // 信号は終わったが、
        // まだ潜望鏡が下まで降りていない
        SetMissionState(
            MissionState.WaitingForSubmerge
        );


        if (debugLog)
        {
            Debug.Log(
                "敵艦の発光信号が終了しました。" +
                "潜望鏡の完全格納を待っています。"
            );
        }
    }


    // ============================================================
    // Button4入力開始
    // ============================================================

    private void BeginSignalInput()
    {
        if (
            currentState !=
                MissionState.Memorizing
            &&
            currentState !=
                MissionState.WaitingForSubmerge
        )
        {
            return;
        }


        submittedSignal.Clear();


        SetMissionState(
            MissionState.Inputting
        );


        if (debugLog)
        {
            if (enemySignalFinished)
            {
                Debug.Log(
                    "潜望鏡完全格納。" +
                    "Button4による信号入力を開始できます。"
                );
            }
            else
            {
                Debug.Log(
                    "潜望鏡完全格納。" +
                    "敵艦の発光途中ですが、" +
                    "Button4による信号入力を開始できます。"
                );
            }
        }
    }


    // ============================================================
    // プレイヤー入力受付
    // ============================================================

    public bool SubmitPlayerSignal(
        IReadOnlyList<SignalSymbol> playerSignal
    )
    {
        if (
            currentState !=
            MissionState.Inputting
        )
        {
            return false;
        }


        if (playerSignal == null)
        {
            return false;
        }


        // 必要記号数と一致していなければ送信しない
        if (
            playerSignal.Count !=
            expectedSignal.Count
        )
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "入力数が必要数と一致しません。" +
                    " Expected=" +
                    expectedSignal.Count +
                    " Input=" +
                    playerSignal.Count
                );
            }


            return false;
        }


        submittedSignal.Clear();


        for (
            int signalIndex = 0;
            signalIndex < playerSignal.Count;
            signalIndex++
        )
        {
            submittedSignal.Add(
                playerSignal[
                    signalIndex
                ]
            );
        }


        if (debugLog)
        {
            Debug.Log(
                "プレイヤー入力完了: " +
                ConvertSignalToString(
                    submittedSignal
                )
            );
        }


        BeginTransmission();


        return true;
    }


    // ============================================================
    // 通信開始
    // ============================================================

    private void BeginTransmission()
    {
        if (
            currentState !=
            MissionState.Inputting
        )
        {
            return;
        }


        SetMissionState(
            MissionState.Transmitting
        );


        // ========================================================
        // 通信マストあり
        // ========================================================

        if (
            communicationMastController !=
            null
        )
        {
            bool started =
                communicationMastController
                    .BeginTransmission(
                        HandleMastTransmissionCompleted
                    );


            if (started)
            {
                if (debugLog)
                {
                    Debug.Log(
                        "通信マストによる送信を開始しました。"
                    );
                }


                return;
            }
        }


        // ========================================================
        // 通信マストが使用できなかった場合
        // ========================================================

        if (
            fallbackTransmissionCoroutine !=
            null
        )
        {
            StopCoroutine(
                fallbackTransmissionCoroutine
            );
        }


        fallbackTransmissionCoroutine =
            StartCoroutine(
                FallbackTransmissionRoutine()
            );
    }


    // ============================================================
    // Mastなし時
    // ============================================================

    private IEnumerator
        FallbackTransmissionRoutine()
    {
        if (
            fallbackTransmissionDuration >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    fallbackTransmissionDuration
                );
        }


        fallbackTransmissionCoroutine =
            null;


        HandleMastTransmissionCompleted();
    }


    // ============================================================
    // Mast通信終了
    // ============================================================

    private void HandleMastTransmissionCompleted()
    {
        if (
            currentState !=
            MissionState.Transmitting
        )
        {
            return;
        }


        BeginEvaluation();
    }


    // ============================================================
    // 正誤判定開始
    // ============================================================

    private void BeginEvaluation()
    {
        if (
            currentState !=
            MissionState.Transmitting
        )
        {
            return;
        }


        SetMissionState(
            MissionState.Evaluating
        );


        if (evaluationCoroutine != null)
        {
            StopCoroutine(
                evaluationCoroutine
            );
        }


        evaluationCoroutine =
            StartCoroutine(
                EvaluationRoutine()
            );
    }


    // ============================================================
    // 正誤判定
    // ============================================================

    private IEnumerator EvaluationRoutine()
    {
        // 「照合中」を画面上で認識できるよう、
        // 少しだけ待つことができる
        if (
            evaluationDelay >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    evaluationDelay
                );
        }


        bool wasSuccessful =
            CompareSignals(
                expectedSignal,
                submittedSignal
            );


        lastMissionWasSuccessful =
            wasSuccessful;


        // ========================================================
        // Score
        // ========================================================

        if (wasSuccessful)
        {
            DataManager.AddScore(
                successScore
            );


            SetMissionState(
                MissionState.Success
            );


            Debug.Log(
                "通信成功！ +" +
                successScore +
                "点"
            );
        }
        else
        {
            DataManager.AddScore(
                failureScore
            );


            SetMissionState(
                MissionState.Failed
            );


            Debug.Log(
                "通信失敗！ " +
                failureScore +
                "点"
            );
        }


        // UIやGameManagerへ通知
        MissionEvaluated?.Invoke(
            wasSuccessful
        );


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


        evaluationCoroutine =
            null;


        FinishMission();
    }


    // ============================================================
    // 信号比較
    // ============================================================

    private bool CompareSignals(
        IReadOnlyList<SignalSymbol> targetSignal,
        IReadOnlyList<SignalSymbol> playerSignal
    )
    {
        if (
            targetSignal == null ||
            playerSignal == null
        )
        {
            return false;
        }


        if (
            targetSignal.Count !=
            playerSignal.Count
        )
        {
            return false;
        }


        for (
            int signalIndex = 0;
            signalIndex < targetSignal.Count;
            signalIndex++
        )
        {
            if (
                targetSignal[signalIndex] !=
                playerSignal[signalIndex]
            )
            {
                return false;
            }
        }


        return true;
    }


    // ============================================================
    // Mission終了
    // ============================================================

    private void FinishMission()
    {
        activeEnemy =
            null;


        expectedSignal.Clear();

        submittedSignal.Clear();


        enemySignalFinished =
            false;


        SetMissionState(
            MissionState.Searching
        );


        if (debugLog)
        {
            Debug.Log(
                "通信ミッション終了。次の敵艦を探索します。"
            );
        }
    }


    // ============================================================
    // Enemy破棄通知
    // ============================================================

    public void NotifyEnemyDestroyed(
        EnemyShip enemyShip
    )
    {
        if (
            enemyShip == null ||
            enemyShip != activeEnemy
        )
        {
            return;
        }


        // ========================================================
        // まだ信号入力前ならMissionキャンセル
        // ========================================================

        if (
            currentState ==
                MissionState.Memorizing
            ||
            currentState ==
                MissionState.WaitingForSubmerge
        )
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "通信対象の敵艦が破棄されたため、" +
                    "通信ミッションを中止します。"
                );
            }


            CancelCurrentMission();
        }


        // Inputting以降では信号パターンを既に保持しているので、
        // EnemyShipが破棄されても処理を継続する。
    }


    // ============================================================
    // Mission強制キャンセル
    // ============================================================

    public void CancelCurrentMission()
    {
        if (evaluationCoroutine != null)
        {
            StopCoroutine(
                evaluationCoroutine
            );


            evaluationCoroutine =
                null;
        }


        if (
            fallbackTransmissionCoroutine !=
            null
        )
        {
            StopCoroutine(
                fallbackTransmissionCoroutine
            );


            fallbackTransmissionCoroutine =
                null;
        }


        activeEnemy =
            null;


        expectedSignal.Clear();

        submittedSignal.Clear();


        enemySignalFinished =
            false;


        SetMissionState(
            MissionState.Searching
        );
    }


    // ============================================================
    // State変更
    // ============================================================

    private void SetMissionState(
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


        if (debugLog)
        {
            Debug.Log(
                "Communication Mission State → " +
                currentState
            );
        }


        MissionStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // Signal → 文字列
    // ============================================================

    private string ConvertSignalToString(
        IReadOnlyList<SignalSymbol> signal
    )
    {
        if (
            signal == null ||
            signal.Count <= 0
        )
        {
            return string.Empty;
        }


        string result =
            string.Empty;


        for (
            int signalIndex = 0;
            signalIndex < signal.Count;
            signalIndex++
        )
        {
            switch (
                signal[signalIndex]
            )
            {
                case SignalSymbol.Short:

                    result +=
                        "・";

                    break;


                case SignalSymbol.Long:

                    result +=
                        "―";

                    break;
            }


            if (
                signalIndex <
                signal.Count - 1
            )
            {
                result +=
                    " ";
            }
        }


        return result;
    }


    // ============================================================
    // Getter
    // ============================================================

    public MissionState GetCurrentState()
    {
        return
            currentState;
    }


    public MissionState GetMissionState()
    {
        return
            currentState;
    }


    public bool GetIsMissionActive()
    {
        return
            currentState !=
            MissionState.Searching;
    }


    public bool GetCanInputSignal()
    {
        return
            currentState ==
            MissionState.Inputting;
    }


    public int GetExpectedSignalCount()
    {
        return
            expectedSignal.Count;
    }


    public IReadOnlyList<SignalSymbol>
        GetExpectedSignalPattern()
    {
        return
            expectedSignal;
    }


    public IReadOnlyList<SignalSymbol>
        GetSubmittedSignalPattern()
    {
        return
            submittedSignal;
    }


    public EnemyShip GetActiveEnemy()
    {
        return
            activeEnemy;
    }


    public bool GetEnemySignalFinished()
    {
        return
            enemySignalFinished;
    }


    public bool GetLastMissionWasSuccessful()
    {
        return
            lastMissionWasSuccessful;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        evaluationDelay =
            Mathf.Max(
                MinimumNonNegativeValue,
                evaluationDelay
            );


        resultStateDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                resultStateDuration
            );


        fallbackTransmissionDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                fallbackTransmissionDuration
            );
    }
}