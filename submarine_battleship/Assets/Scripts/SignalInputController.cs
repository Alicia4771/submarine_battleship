using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SignalInputController : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultLongPressThreshold =
        0.40f;

    private const float MinimumLongPressThreshold =
        0.01f;


    // ============================================================
    // Event
    // ============================================================

    /// <summary>
    /// 信号入力モードの開始・終了時に通知する。
    /// true = 入力受付中
    /// false = 入力受付終了
    /// </summary>
    public event Action<bool>
        InputModeChanged;


    /// <summary>
    /// 確定済みの入力信号が変化したときに通知する。
    /// 第1引数 = 現在までに入力された信号
    /// 第2引数 = 今回必要な信号数
    /// </summary>
    public event Action<
        IReadOnlyList<SignalSymbol>,
        int
    >
        EnteredSignalsChanged;


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField, Tooltip(
        "通信ミッションを管理するCommunicationMissionManager。" +
        "未設定なら自動検索する")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // Button4
    // ============================================================

    [Header("Button 4")]

    [SerializeField, Tooltip(
        "この時間未満のButton4押下を短信号「・」とする。" +
        "この時間以上なら長信号「―」")]
    [Min(MinimumLongPressThreshold)]
    private float longPressThreshold =
        DefaultLongPressThreshold;


    [SerializeField, Tooltip(
        "入力受付開始時にButton4が既に押されていた場合、" +
        "一度離すまで入力として扱わない")]
    private bool requireReleaseBeforeFirstInput =
        true;


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

    private readonly List<SignalSymbol>
        enteredSignals =
            new List<SignalSymbol>();


    private bool inputEnabled =
        false;


    private bool previousButtonPressed =
        false;


    private bool measuringPress =
        false;


    private bool waitingForInitialRelease =
        false;


    private float pressStartTime =
        0.0f;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();
    }


    // ============================================================
    // OnEnable
    // ============================================================

    private void OnEnable()
    {
        ResolveReferences();

        SubscribeEvents();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        SubscribeEvents();


        if (
            communicationMissionManager !=
            null
        )
        {
            HandleMissionStateChanged(
                communicationMissionManager
                    .GetCurrentState()
            );
        }
    }


    // ============================================================
    // OnDisable
    // ============================================================

    private void OnDisable()
    {
        UnsubscribeEvents();

        ResetButtonState();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            ResolveReferences();


            if (
                communicationMissionManager ==
                null
            )
            {
                return;
            }
        }


        // ========================================================
        // Inputting以外ではButton4を受け付けない
        // ========================================================

        if (
            !inputEnabled ||
            communicationMissionManager
                .GetCurrentState()
            !=
            CommunicationMissionManager
                .MissionState
                .Inputting
        )
        {
            SyncButtonState();

            return;
        }


        // 管理者メニュー等でTimeScale=0なら
        // ゲーム入力として扱わない
        if (
            Time.timeScale <=
            Mathf.Epsilon
        )
        {
            CancelCurrentPress();

            SyncButtonState();

            return;
        }


        bool buttonPressed =
            GetButton4Pressed();


        // ========================================================
        // 入力開始時に既にボタンが押されていた場合
        // ========================================================

        if (waitingForInitialRelease)
        {
            if (!buttonPressed)
            {
                waitingForInitialRelease =
                    false;


                previousButtonPressed =
                    false;


                if (debugLog)
                {
                    Debug.Log(
                        "Button4入力受付開始。"
                    );
                }
            }
            else
            {
                previousButtonPressed =
                    true;
            }


            return;
        }


        // ========================================================
        // 押した瞬間
        // ========================================================

        if (
            buttonPressed &&
            !previousButtonPressed
        )
        {
            BeginPress();
        }


        // ========================================================
        // 離した瞬間
        // ========================================================

        if (
            !buttonPressed &&
            previousButtonPressed
        )
        {
            EndPress();
        }


        previousButtonPressed =
            buttonPressed;
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (
            communicationMissionManager !=
            null
        )
        {
            return;
        }


        communicationMissionManager =
            FindFirstObjectByType<
                CommunicationMissionManager
            >();


        if (
            communicationMissionManager ==
                null
            &&
            debugLog
        )
        {
            Debug.LogWarning(
                "SignalInputController: " +
                "CommunicationMissionManagerが見つかりません。"
            );
        }
    }


    // ============================================================
    // Event登録
    // ============================================================

    private void SubscribeEvents()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        // 二重登録防止
        communicationMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;


        communicationMissionManager
            .MissionStateChanged +=
                HandleMissionStateChanged;
    }


    // ============================================================
    // Event解除
    // ============================================================

    private void UnsubscribeEvents()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        communicationMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;
    }


    // ============================================================
    // Mission状態変更
    // ============================================================

    private void HandleMissionStateChanged(
        CommunicationMissionManager
            .MissionState newState
    )
    {
        switch (newState)
        {
            // ====================================================
            // 入力可能
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Inputting:

                BeginInputMode();

                break;


            // ====================================================
            // それ以外
            // ====================================================

            default:

                EndInputMode();

                break;
        }
    }


    // ============================================================
    // 入力モード開始
    // ============================================================

    private void BeginInputMode()
    {
        enteredSignals.Clear();


        inputEnabled =
            true;


        InputModeChanged?.Invoke(
            true
        );


        NotifyEnteredSignalsChanged();


        measuringPress =
            false;


        bool currentlyPressed =
            GetButton4Pressed();


        previousButtonPressed =
            currentlyPressed;


        waitingForInitialRelease =
            requireReleaseBeforeFirstInput &&
            currentlyPressed;


        if (debugLog)
        {
            Debug.Log(
                "信号入力受付開始。" +
                " 必要記号数=" +
                GetExpectedSignalCount()
            );
        }
    }


    // ============================================================
    // 入力モード終了
    // ============================================================

    private void EndInputMode()
    {
        bool wasInputEnabled =
            inputEnabled;


        inputEnabled =
            false;


        CancelCurrentPress();


        SyncButtonState();


        if (wasInputEnabled)
        {
            InputModeChanged?.Invoke(
                false
            );
        }
    }


    // ============================================================
    // 押下開始
    // ============================================================

    private void BeginPress()
    {
        if (!inputEnabled)
        {
            return;
        }


        measuringPress =
            true;


        pressStartTime =
            Time.unscaledTime;
    }


    // ============================================================
    // 押下終了
    // ============================================================

    private void EndPress()
    {
        if (!measuringPress)
        {
            return;
        }


        measuringPress =
            false;


        float pressDuration =
            Time.unscaledTime -
            pressStartTime;


        SignalSymbol inputSymbol;


        if (
            pressDuration >=
            longPressThreshold
        )
        {
            inputSymbol =
                SignalSymbol.Long;
        }
        else
        {
            inputSymbol =
                SignalSymbol.Short;
        }


        RegisterSignal(
            inputSymbol,
            pressDuration
        );
    }


    // ============================================================
    // 信号登録
    // ============================================================

    private void RegisterSignal(
        SignalSymbol signalSymbol,
        float pressDuration
    )
    {
        if (!inputEnabled)
        {
            return;
        }


        int expectedCount =
            GetExpectedSignalCount();


        if (expectedCount <= 0)
        {
            return;
        }


        if (
            enteredSignals.Count >=
            expectedCount
        )
        {
            return;
        }


        enteredSignals.Add(
            signalSymbol
        );


        NotifyEnteredSignalsChanged();


        if (debugLog)
        {
            Debug.Log(
                "Button4入力: " +
                (
                    signalSymbol ==
                    SignalSymbol.Short
                        ? "・"
                        : "―"
                )
                +
                "  押下時間=" +
                pressDuration.ToString("0.000") +
                "秒"
                +
                "  [" +
                enteredSignals.Count +
                "/" +
                expectedCount +
                "]"
            );
        }


        // ========================================================
        // 必要数入力完了
        // ========================================================

        if (
            enteredSignals.Count >=
            expectedCount
        )
        {
            CompleteInput();
        }
    }


    // ============================================================
    // 入力完了
    // ============================================================

    private void CompleteInput()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        inputEnabled =
            false;


        bool accepted =
            communicationMissionManager
                .SubmitPlayerSignal(
                    enteredSignals
                );


        if (!accepted)
        {
            // 何らかの理由でMissionManagerが
            // 受け付けられなかった場合
            inputEnabled =
                communicationMissionManager
                    .GetCurrentState()
                ==
                CommunicationMissionManager
                    .MissionState
                    .Inputting;

            return;
        }


        // SubmitPlayerSignalが成功すると
        // MissionStateはTransmittingへ移行する。
        // CompleteInput冒頭でinputEnabledをfalseにしているため、
        // UIへはここで明示的に入力終了を通知する。
        InputModeChanged?.Invoke(
            false
        );
    }


    // ============================================================
    // Button4取得
    // ============================================================

    private bool GetButton4Pressed()
    {
        return
            DataManager
                .GetSensorButton4()
            ==
            1;
    }


    // ============================================================
    // Button状態同期
    // ============================================================

    private void SyncButtonState()
    {
        previousButtonPressed =
            GetButton4Pressed();
    }


    // ============================================================
    // 現在の押下キャンセル
    // ============================================================

    private void CancelCurrentPress()
    {
        measuringPress =
            false;


        pressStartTime =
            0.0f;
    }


    // ============================================================
    // Button状態リセット
    // ============================================================

    private void ResetButtonState()
    {
        bool wasInputEnabled =
            inputEnabled;


        inputEnabled =
            false;


        measuringPress =
            false;


        waitingForInitialRelease =
            false;


        previousButtonPressed =
            false;


        pressStartTime =
            0.0f;


        if (wasInputEnabled)
        {
            InputModeChanged?.Invoke(
                false
            );
        }
    }


    // ============================================================
    // UIなどへ現在の入力内容を通知
    // ============================================================

    private void NotifyEnteredSignalsChanged()
    {
        EnteredSignalsChanged?.Invoke(
            enteredSignals,
            GetExpectedSignalCount()
        );
    }


    // ============================================================
    // 必要数
    // ============================================================

    private int GetExpectedSignalCount()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return 0;
        }


        return
            communicationMissionManager
                .GetExpectedSignalCount();
    }


    // ============================================================
    // Debug用
    // ============================================================

    public void AddShortSignalForDebug()
    {
        if (!inputEnabled)
        {
            return;
        }


        RegisterSignal(
            SignalSymbol.Short,
            0.0f
        );
    }


    public void AddLongSignalForDebug()
    {
        if (!inputEnabled)
        {
            return;
        }


        RegisterSignal(
            SignalSymbol.Long,
            longPressThreshold
        );
    }


    // ============================================================
    // Getter
    // ============================================================

    public bool GetIsInputEnabled()
    {
        return
            inputEnabled;
    }


    public int GetEnteredSignalCount()
    {
        return
            enteredSignals.Count;
    }


    public IReadOnlyList<SignalSymbol>
        GetEnteredSignals()
    {
        return
            enteredSignals;
    }


    public int GetExpectedSignalCountForDisplay()
    {
        return
            GetExpectedSignalCount();
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        longPressThreshold =
            Mathf.Max(
                MinimumLongPressThreshold,
                longPressThreshold
            );
    }
}