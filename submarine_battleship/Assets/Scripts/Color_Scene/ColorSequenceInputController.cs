using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorSequenceInputController : MonoBehaviour
{
    public event Action<bool> InputModeChanged;
    public event Action<IReadOnlyList<ColorSignalSymbol>, int> EnteredColorsChanged;

    [Header("Mission")]
    [SerializeField]
    private ColorMemoryMissionManager colorMemoryMissionManager;

    [Header("Input")]
    [SerializeField, Tooltip(
        "入力開始時にButton2～4のどれかが押されていた場合、" +
        "一度すべて離すまで入力しない")]
    private bool requireReleaseBeforeFirstInput = true;

    [SerializeField, Tooltip(
        "最後の色を入力したあと、Button2～4がすべて離されるまで" +
        "入力完了にせず、潜望鏡操作を再開しない")]
    private bool requireReleaseAfterFinalInput = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    private readonly List<ColorSignalSymbol> enteredColors =
        new List<ColorSignalSymbol>();

    private bool inputEnabled = false;
    private bool waitingForInitialRelease = false;
    private bool waitingForFinalRelease = false;

    private int previousButton2 = 0;
    private int previousButton3 = 0;
    private int previousButton4 = 0;
    private int previousButton6 = 0;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeEvents();

        if (colorMemoryMissionManager != null)
        {
            HandleMissionStateChanged(
                colorMemoryMissionManager.GetCurrentState()
            );
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        ResetInputState();
    }

    private void Update()
    {
        if (
            !inputEnabled ||
            colorMemoryMissionManager == null
        )
        {
            SyncButtonState();
            return;
        }

        if (
            colorMemoryMissionManager.GetCurrentState() !=
            ColorMemoryMissionManager.MissionState.Inputting
        )
        {
            SyncButtonState();
            return;
        }

        if (Time.timeScale <= Mathf.Epsilon)
        {
            SyncButtonState();
            return;
        }

        int currentButton2 =
            DataManager.GetSensorButton2();

        int currentButton3 =
            DataManager.GetSensorButton3();

        int currentButton4 =
            DataManager.GetSensorButton4();

        int currentButton6 =
            DataManager.GetSensorButton6();

        if (waitingForInitialRelease)
        {
            bool allReleased =
                AreColorButtonsReleased(
                    currentButton2,
                    currentButton3,
                    currentButton4
                );

            previousButton2 = currentButton2;
            previousButton3 = currentButton3;
            previousButton4 = currentButton4;
            previousButton6 = currentButton6;

            if (allReleased)
            {
                waitingForInitialRelease = false;

                if (debugLog)
                {
                    Debug.Log(
                        "色入力開始前のボタン解放を確認しました。"
                    );
                }
            }

            return;
        }

        bool button6Pressed =
            currentButton6 == 1 &&
            previousButton6 != 1;

        if (button6Pressed)
        {
            ResetEnteredColors(
                currentButton2,
                currentButton3,
                currentButton4
            );

            previousButton6 = currentButton6;

            return;
        }

        if (waitingForFinalRelease)
        {
            bool allReleased =
                AreColorButtonsReleased(
                    currentButton2,
                    currentButton3,
                    currentButton4
                );

            previousButton2 = currentButton2;
            previousButton3 = currentButton3;
            previousButton4 = currentButton4;
            previousButton6 = currentButton6;

            if (!allReleased)
            {
                return;
            }

            waitingForFinalRelease = false;

            if (debugLog)
            {
                Debug.Log(
                    "最後の色入力後のボタン解放を確認しました。" +
                    "通信処理へ進みます。"
                );
            }

            CompleteInput();
            return;
        }

        bool button2Pressed =
            currentButton2 == 1 &&
            previousButton2 != 1;

        bool button3Pressed =
            currentButton3 == 1 &&
            previousButton3 != 1;

        bool button4Pressed =
            currentButton4 == 1 &&
            previousButton4 != 1;

        if (button2Pressed)
        {
            RegisterColor(
                ColorSignalSymbol.Red
            );
        }
        else if (button3Pressed)
        {
            RegisterColor(
                ColorSignalSymbol.Blue
            );
        }
        else if (button4Pressed)
        {
            RegisterColor(
                ColorSignalSymbol.Yellow
            );
        }

        previousButton2 = currentButton2;
        previousButton3 = currentButton3;
        previousButton4 = currentButton4;
        previousButton6 = currentButton6;
    }

    private void ResolveReferences()
    {
        if (colorMemoryMissionManager == null)
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<
                    ColorMemoryMissionManager
                >();
        }
    }

    private void SubscribeEvents()
    {
        if (colorMemoryMissionManager == null)
        {
            return;
        }

        colorMemoryMissionManager.MissionStateChanged -=
            HandleMissionStateChanged;

        colorMemoryMissionManager.MissionStateChanged +=
            HandleMissionStateChanged;
    }

    private void UnsubscribeEvents()
    {
        if (colorMemoryMissionManager == null)
        {
            return;
        }

        colorMemoryMissionManager.MissionStateChanged -=
            HandleMissionStateChanged;
    }

    private void HandleMissionStateChanged(
        ColorMemoryMissionManager.MissionState newState
    )
    {
        if (
            newState ==
            ColorMemoryMissionManager.MissionState.Inputting
        )
        {
            BeginInputMode();
            return;
        }

        EndInputMode();
    }

    private void BeginInputMode()
    {
        enteredColors.Clear();

        inputEnabled = true;
        waitingForFinalRelease = false;

        int currentButton2 =
            DataManager.GetSensorButton2();

        int currentButton3 =
            DataManager.GetSensorButton3();

        int currentButton4 =
            DataManager.GetSensorButton4();

        int currentButton6 =
            DataManager.GetSensorButton6();

        previousButton2 = currentButton2;
        previousButton3 = currentButton3;
        previousButton4 = currentButton4;
        previousButton6 = currentButton6;

        waitingForInitialRelease =
            requireReleaseBeforeFirstInput &&
            (
                currentButton2 == 1 ||
                currentButton3 == 1 ||
                currentButton4 == 1
            );

        InputModeChanged?.Invoke(true);

        NotifyEnteredColorsChanged();

        if (debugLog)
        {
            Debug.Log(
                "色入力受付開始。必要色数=" +
                GetExpectedColorCount()
            );
        }
    }

    private void EndInputMode()
    {
        bool wasInputEnabled =
            inputEnabled;

        inputEnabled = false;
        waitingForInitialRelease = false;
        waitingForFinalRelease = false;

        SyncButtonState();

        if (wasInputEnabled)
        {
            InputModeChanged?.Invoke(false);
        }
    }

    private void RegisterColor(
        ColorSignalSymbol color
    )
    {
        if (!inputEnabled)
        {
            return;
        }

        if (waitingForFinalRelease)
        {
            return;
        }

        int expectedCount =
            GetExpectedColorCount();

        if (
            expectedCount <= 0 ||
            enteredColors.Count >= expectedCount
        )
        {
            return;
        }

        enteredColors.Add(color);

        NotifyEnteredColorsChanged();

        if (debugLog)
        {
            Debug.Log(
                "色入力: " +
                GetColorName(color) +
                " [" +
                enteredColors.Count +
                "/" +
                expectedCount +
                "]"
            );
        }

        if (
            enteredColors.Count <
            expectedCount
        )
        {
            return;
        }

        if (requireReleaseAfterFinalInput)
        {
            waitingForFinalRelease = true;

            if (debugLog)
            {
                Debug.Log(
                    "最後の色を入力しました。" +
                    "Button2～4がすべて離されるまで待機します。"
                );
            }

            return;
        }

        CompleteInput();
    }

    private void CompleteInput()
    {
        if (colorMemoryMissionManager == null)
        {
            return;
        }

        bool accepted =
            colorMemoryMissionManager
                .SubmitPlayerSequence(
                    enteredColors
                );

        if (!accepted)
        {
            inputEnabled =
                colorMemoryMissionManager
                    .GetCurrentState()
                ==
                ColorMemoryMissionManager
                    .MissionState
                    .Inputting;
        }
    }

    private void ResetEnteredColors(
        int currentButton2,
        int currentButton3,
        int currentButton4
    )
    {
        enteredColors.Clear();

        waitingForFinalRelease = false;

        waitingForInitialRelease =
            currentButton2 == 1 ||
            currentButton3 == 1 ||
            currentButton4 == 1;

        previousButton2 = currentButton2;
        previousButton3 = currentButton3;
        previousButton4 = currentButton4;

        NotifyEnteredColorsChanged();

        if (debugLog)
        {
            Debug.Log(
                "Button6: 現在入力している色信号をリセットしました。"
            );
        }
    }


    private bool AreColorButtonsReleased(
        int button2,
        int button3,
        int button4
    )
    {
        return
            button2 == 0 &&
            button3 == 0 &&
            button4 == 0;
    }

    private void SyncButtonState()
    {
        previousButton2 =
            DataManager.GetSensorButton2();

        previousButton3 =
            DataManager.GetSensorButton3();

        previousButton4 =
            DataManager.GetSensorButton4();

        previousButton6 =
            DataManager.GetSensorButton6();
    }

    private void NotifyEnteredColorsChanged()
    {
        EnteredColorsChanged?.Invoke(
            enteredColors,
            GetExpectedColorCount()
        );
    }

    private int GetExpectedColorCount()
    {
        if (colorMemoryMissionManager == null)
        {
            return 0;
        }

        return
            colorMemoryMissionManager
                .GetExpectedColorCount();
    }

    public IReadOnlyList<ColorSignalSymbol>
        GetEnteredColors()
    {
        return enteredColors;
    }

    public int GetExpectedColorCountForDisplay()
    {
        return GetExpectedColorCount();
    }

    public bool GetIsInputEnabled()
    {
        return inputEnabled;
    }

    public bool GetIsWaitingForFinalRelease()
    {
        return waitingForFinalRelease;
    }

    private string GetColorName(
        ColorSignalSymbol color
    )
    {
        switch (color)
        {
            case ColorSignalSymbol.Red:
                return "赤";

            case ColorSignalSymbol.Blue:
                return "青";

            case ColorSignalSymbol.Yellow:
                return "黄";

            default:
                return "?";
        }
    }

    private void ResetInputState()
    {
        inputEnabled = false;
        waitingForInitialRelease = false;
        waitingForFinalRelease = false;

        previousButton2 = 0;
        previousButton3 = 0;
        previousButton4 = 0;
        previousButton6 = 0;
    }
}
