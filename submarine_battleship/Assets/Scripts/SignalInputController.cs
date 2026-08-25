using System.Collections.Generic;
using UnityEngine;

public class SignalInputController : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const int ButtonReleased = 0;
    private const int ButtonPressed = 1;

    private const float DefaultMinimumValidPressDuration = 0.05f;

    private const float DefaultLongPressThreshold = 0.35f;

    private const float MinimumNonNegativeValue = 0.0f;


    // ============================================================
    // Inspector設定
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "通信ミッションを管理するManager。" +
        "未設定の場合は自動検索する")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // Button4判定
    // ============================================================

    [Header("Signal Button")]

    [SerializeField, Tooltip(
        "これより短い押下は誤入力として無視する")]
    [Min(MinimumNonNegativeValue)]
    private float minimumValidPressDuration =
        DefaultMinimumValidPressDuration;


    [SerializeField, Tooltip(
        "この時間以上押した場合を長信号と判定する")]
    [Min(MinimumNonNegativeValue)]
    private float longPressThreshold =
        DefaultLongPressThreshold;


    // ============================================================
    // デバッグ
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "Button4の入力結果をConsoleへ表示する")]
    private bool debugLog =
        true;


    // ============================================================
    // 内部状態
    // ============================================================

    private readonly List<SignalSymbol>
        inputSignal =
            new();


    private int previousButtonState =
        ButtonReleased;


    private bool isPressing =
        false;


    // 通信入力状態へ入った時点で
    // Button4が押しっぱなしだった場合の
    // 誤入力防止用
    private bool inputArmed =
        false;


    private float pressStartedTime =
        0.0f;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            communicationMissionManager =
                FindFirstObjectByType<
                    CommunicationMissionManager
                >();
        }


        previousButtonState =
            DataManager
                .GetSensorButton4();


        inputArmed =
            previousButtonState ==
            ButtonReleased;
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
            return;
        }


        int currentButtonState =
            DataManager
                .GetSensorButton4();


        // =========================
        // 通信入力期間外
        // =========================

        if (
            !communicationMissionManager
                .CanAcceptSignalInput()
        )
        {
            CancelCurrentPress();


            previousButtonState =
                currentButtonState;


            // Button4が離されていれば
            // 次回Inputtingになった時に使用可能
            inputArmed =
                currentButtonState ==
                ButtonReleased;


            return;
        }


        // =========================
        // 押しっぱなし開始防止
        // =========================

        if (!inputArmed)
        {
            if (
                currentButtonState ==
                ButtonReleased
            )
            {
                inputArmed =
                    true;
            }


            previousButtonState =
                currentButtonState;


            return;
        }


        // =========================
        // 0 → 1
        // =========================

        bool buttonPressedThisFrame =
            currentButtonState ==
            ButtonPressed &&
            previousButtonState ==
            ButtonReleased;


        if (buttonPressedThisFrame)
        {
            BeginPress();
        }


        // =========================
        // 1 → 0
        // =========================

        bool buttonReleasedThisFrame =
            currentButtonState ==
            ButtonReleased &&
            previousButtonState ==
            ButtonPressed;


        if (buttonReleasedThisFrame)
        {
            EndPress();
        }


        previousButtonState =
            currentButtonState;
    }


    // ============================================================
    // 押下開始
    // ============================================================

    private void BeginPress()
    {
        if (isPressing)
        {
            return;
        }


        isPressing =
            true;


        pressStartedTime =
            Time.unscaledTime;
    }


    // ============================================================
    // 押下終了
    // ============================================================

    private void EndPress()
    {
        if (!isPressing)
        {
            return;
        }


        isPressing =
            false;


        float pressDuration =
            Time.unscaledTime -
            pressStartedTime;


        // =========================
        // 短すぎる入力
        // =========================

        if (
            pressDuration <
            minimumValidPressDuration
        )
        {
            if (debugLog)
            {
                Debug.Log(
                    "Button4入力が短すぎるため無視: " +
                    pressDuration +
                    "秒"
                );
            }


            return;
        }


        // =========================
        // 短・長判定
        // =========================

        SignalSymbol symbol;


        if (
            pressDuration >=
            longPressThreshold
        )
        {
            symbol =
                SignalSymbol.Long;
        }
        else
        {
            symbol =
                SignalSymbol.Short;
        }


        AddSignalSymbol(
            symbol,
            pressDuration
        );
    }


    // ============================================================
    // 信号追加
    // ============================================================

    private void AddSignalSymbol(
        SignalSymbol symbol,
        float pressDuration
    )
    {
        inputSignal.Add(
            symbol
        );


        if (debugLog)
        {
            string symbolText =
                symbol ==
                SignalSymbol.Short
                    ? "・"
                    : "―";


            Debug.Log(
                "信号入力: " +
                symbolText +
                " (" +
                pressDuration.ToString(
                    "F3"
                ) +
                "秒)"
            );
        }


        CheckInputCompleted();
    }


    // ============================================================
    // 入力完了判定
    // ============================================================

    private void CheckInputCompleted()
    {
        int expectedLength =
            communicationMissionManager
                .GetExpectedSignalLength();


        if (expectedLength <= 0)
        {
            return;
        }


        if (
            inputSignal.Count <
            expectedLength
        )
        {
            return;
        }


        // =========================
        // MissionManagerへコピー送信
        // =========================

        List<SignalSymbol>
            submittedSignal =
                new(
                    inputSignal
                );


        // これ以上入力させない
        inputArmed =
            false;


        communicationMissionManager
            .SubmitPlayerSignal(
                submittedSignal
            );
    }


    // ============================================================
    // 入力リセット
    // ============================================================

    public void ClearInput()
    {
        inputSignal.Clear();


        CancelCurrentPress();


        int currentButtonState =
            DataManager
                .GetSensorButton4();


        previousButtonState =
            currentButtonState;


        inputArmed =
            currentButtonState ==
            ButtonReleased;
    }


    private void CancelCurrentPress()
    {
        isPressing =
            false;


        pressStartedTime =
            0.0f;
    }


    // ============================================================
    // 状態取得
    // ============================================================

    public int GetCurrentInputCount()
    {
        return
            inputSignal.Count;
    }


    public IReadOnlyList<SignalSymbol>
        GetCurrentInputSignal()
    {
        return
            inputSignal;
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        minimumValidPressDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                minimumValidPressDuration
            );


        longPressThreshold =
            Mathf.Max(
                minimumValidPressDuration,
                longPressThreshold
            );
    }
}