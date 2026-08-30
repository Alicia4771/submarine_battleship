using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorMemoryMissionManager : MonoBehaviour
{
    // ============================================================
    // Mission State
    // ============================================================

    public enum MissionState
    {
        Searching = 0,
        Memorizing = 1,
        WaitingForSubmerge = 2,
        Inputting = 3,
        Transmitting = 4,
        Evaluating = 5,
        Success = 6,
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

    public event Action<MissionState>
        MissionStateChanged;

    public event Action<bool>
        MissionEvaluated;


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "通信マストを制御するCommunicationMastController。" +
        "未設定の場合は自動検索する")]
    private CommunicationMastController
        communicationMastController;


    [SerializeField, Tooltip(
        "潜望鏡を制御するPeriscopeController。" +
        "色入力中だけ潜望鏡の上下操作を停止するために使用する。" +
        "未設定の場合は自動検索する")]
    private PeriscopeController
        periscopeController;


    // ============================================================
    // Score
    // ============================================================

    [Header("Score")]

    [SerializeField]
    private int successScore =
        DefaultSuccessScore;


    [SerializeField]
    private int failureScore =
        DefaultFailureScore;


    // ============================================================
    // Timing
    // ============================================================

    [Header("Timing")]

    [SerializeField, Min(MinimumNonNegativeValue)]
    private float evaluationDelay =
        DefaultEvaluationDelay;


    [SerializeField, Min(MinimumNonNegativeValue)]
    private float resultStateDuration =
        DefaultResultStateDuration;


    [SerializeField, Min(MinimumNonNegativeValue)]
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
    // Internal
    // ============================================================

    [SerializeField]
    private MissionState currentState =
        MissionState.Searching;


    private ColorMemoryEnemyShip
        activeEnemy;


    private readonly List<ColorSignalSymbol>
        expectedSequence =
            new List<ColorSignalSymbol>();


    private readonly List<ColorSignalSymbol>
        submittedSequence =
            new List<ColorSignalSymbol>();


    private bool enemySequenceFinished =
        false;


    private bool lastMissionWasSuccessful =
        false;


    private Coroutine evaluationCoroutine;

    private Coroutine fallbackTransmissionCoroutine;


    // 潜望鏡を色入力のために停止したか
    private bool periscopeStoppedByColorInput =
        false;


    // 停止前にPeriscopeControllerが有効だったか
    private bool periscopeWasEnabled =
        true;


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
    // OnDisable
    // ============================================================

    private void OnDisable()
    {
        RestorePeriscopeController();
    }


    // ============================================================
    // OnDestroy
    // ============================================================

    private void OnDestroy()
    {
        RestorePeriscopeController();
    }


    // ============================================================
    // References
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
            periscopeController ==
            null
        )
        {
            periscopeController =
                FindFirstObjectByType<
                    PeriscopeController
                >();
        }


        if (
            communicationMastController ==
            null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "ColorMemoryMissionManager: " +
                "CommunicationMastControllerが見つかりません。"
            );
        }


        if (
            periscopeController ==
            null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "ColorMemoryMissionManager: " +
                "PeriscopeControllerが見つかりません。"
            );
        }
    }


    // ============================================================
    // 潜望鏡完全格納監視
    // ============================================================

    private void UpdatePeriscopeLoweredState()
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


        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            return;
        }


        BeginColorInput();
    }


    // ============================================================
    // Mission開始
    // ============================================================

    public bool TryBeginMission(
        ColorMemoryEnemyShip enemyShip,
        IReadOnlyList<ColorSignalSymbol>
            colorSequence
    )
    {
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
            colorSequence == null ||
            colorSequence.Count <= 0
        )
        {
            return false;
        }


        activeEnemy =
            enemyShip;


        expectedSequence.Clear();


        for (
            int index = 0;
            index < colorSequence.Count;
            index++
        )
        {
            expectedSequence.Add(
                colorSequence[index]
            );
        }


        submittedSequence.Clear();


        enemySequenceFinished =
            false;


        lastMissionWasSuccessful =
            false;


        SetMissionState(
            MissionState.Memorizing
        );


        if (debugLog)
        {
            Debug.Log(
                "色記憶ミッション開始: " +
                ConvertSequenceToString(
                    expectedSequence
                )
            );
        }


        return true;
    }


    // ============================================================
    // 敵船の色列終了
    // ============================================================

    public void NotifyEnemySequenceFinished(
        ColorMemoryEnemyShip enemyShip
    )
    {
        if (
            enemyShip == null ||
            enemyShip != activeEnemy
        )
        {
            return;
        }


        enemySequenceFinished =
            true;


        // すでに入力以降へ進んでいる場合は
        // 状態を巻き戻さない
        if (
            currentState !=
            MissionState.Memorizing
        )
        {
            return;
        }


        if (
            DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            BeginColorInput();

            return;
        }


        SetMissionState(
            MissionState.WaitingForSubmerge
        );
    }


    // ============================================================
    // 色入力開始
    // ============================================================

    private void BeginColorInput()
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


        submittedSequence.Clear();


        SetMissionState(
            MissionState.Inputting
        );


        StopPeriscopeController();


        if (debugLog)
        {
            Debug.Log(
                "色入力開始: " +
                "Button2=赤 / Button3=青 / Button4=黄"
            );
        }
    }


    // ============================================================
    // 潜望鏡停止
    // ============================================================

    private void StopPeriscopeController()
    {
        if (periscopeController == null)
        {
            ResolveReferences();
        }


        if (periscopeController == null)
        {
            return;
        }


        if (periscopeStoppedByColorInput)
        {
            return;
        }


        periscopeWasEnabled =
            periscopeController.enabled;


        periscopeStoppedByColorInput =
            true;


        if (periscopeWasEnabled)
        {
            periscopeController.enabled =
                false;
        }
    }


    // ============================================================
    // 潜望鏡復帰
    // ============================================================

    private void RestorePeriscopeController()
    {
        if (!periscopeStoppedByColorInput)
        {
            return;
        }


        if (
            periscopeController !=
            null &&
            periscopeWasEnabled
        )
        {
            periscopeController.enabled =
                true;


            // 色入力中にセンサーYawが変化していても
            // 復帰時に潜望鏡が突然回らないよう再基準化
            periscopeController
                .RecenterYaw();
        }


        periscopeStoppedByColorInput =
            false;
    }


    // ============================================================
    // プレイヤー入力
    // ============================================================

    public bool SubmitPlayerSequence(
        IReadOnlyList<ColorSignalSymbol>
            playerSequence
    )
    {
        if (
            currentState !=
            MissionState.Inputting
        )
        {
            return false;
        }


        if (playerSequence == null)
        {
            return false;
        }


        if (
            playerSequence.Count !=
            expectedSequence.Count
        )
        {
            return false;
        }


        submittedSequence.Clear();


        for (
            int index = 0;
            index < playerSequence.Count;
            index++
        )
        {
            submittedSequence.Add(
                playerSequence[index]
            );
        }


        RestorePeriscopeController();


        BeginTransmission();


        return true;
    }


    // ============================================================
    // 通信
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
                return;
            }
        }


        fallbackTransmissionCoroutine =
            StartCoroutine(
                FallbackTransmissionRoutine()
            );
    }


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
    // Mast終了
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
    // 評価
    // ============================================================

    private void BeginEvaluation()
    {
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


    private IEnumerator EvaluationRoutine()
    {
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
            CompareSequences(
                expectedSequence,
                submittedSequence
            );


        lastMissionWasSuccessful =
            wasSuccessful;


        if (wasSuccessful)
        {
            DataManager.AddScore(
                successScore
            );


            SetMissionState(
                MissionState.Success
            );


            Debug.Log(
                "色記憶通信成功！ +" +
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
                "色記憶通信失敗！ " +
                failureScore +
                "点"
            );
        }


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
    // 比較
    // ============================================================

    private bool CompareSequences(
        IReadOnlyList<ColorSignalSymbol>
            targetSequence,
        IReadOnlyList<ColorSignalSymbol>
            playerSequence
    )
    {
        if (
            targetSequence == null ||
            playerSequence == null
        )
        {
            return false;
        }


        if (
            targetSequence.Count !=
            playerSequence.Count
        )
        {
            return false;
        }


        for (
            int index = 0;
            index < targetSequence.Count;
            index++
        )
        {
            if (
                targetSequence[index] !=
                playerSequence[index]
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
        RestorePeriscopeController();


        activeEnemy =
            null;


        expectedSequence.Clear();

        submittedSequence.Clear();


        enemySequenceFinished =
            false;


        SetMissionState(
            MissionState.Searching
        );
    }


    // ============================================================
    // Enemy破棄
    // ============================================================

    public void NotifyEnemyDestroyed(
        ColorMemoryEnemyShip enemyShip
    )
    {
        if (
            enemyShip == null ||
            enemyShip != activeEnemy
        )
        {
            return;
        }


        if (
            currentState ==
                MissionState.Memorizing
            ||
            currentState ==
                MissionState.WaitingForSubmerge
        )
        {
            CancelCurrentMission();
        }
    }


    // ============================================================
    // Missionキャンセル
    // ============================================================

    public void CancelCurrentMission()
    {
        RestorePeriscopeController();


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


        expectedSequence.Clear();

        submittedSequence.Clear();


        enemySequenceFinished =
            false;


        SetMissionState(
            MissionState.Searching
        );
    }


    // ============================================================
    // State
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
                "ColorMemory Mission State → " +
                currentState
            );
        }


        MissionStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // Debug文字列
    // ============================================================

    private string ConvertSequenceToString(
        IReadOnlyList<ColorSignalSymbol>
            sequence
    )
    {
        string result =
            string.Empty;


        for (
            int index = 0;
            index < sequence.Count;
            index++
        )
        {
            switch (sequence[index])
            {
                case ColorSignalSymbol.Red:

                    result +=
                        "赤";

                    break;


                case ColorSignalSymbol.Blue:

                    result +=
                        "青";

                    break;


                case ColorSignalSymbol.Yellow:

                    result +=
                        "黄";

                    break;
            }


            if (
                index <
                sequence.Count - 1
            )
            {
                result +=
                    " → ";
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


    public int GetExpectedColorCount()
    {
        return
            expectedSequence.Count;
    }


    public IReadOnlyList<ColorSignalSymbol>
        GetExpectedColorSequence()
    {
        return
            expectedSequence;
    }


    public IReadOnlyList<ColorSignalSymbol>
        GetSubmittedColorSequence()
    {
        return
            submittedSequence;
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