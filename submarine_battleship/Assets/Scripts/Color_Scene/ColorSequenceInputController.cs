using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorSequenceInputController : MonoBehaviour
{
    // ============================================================
    // Event
    // ============================================================

    public event Action<bool>
        InputModeChanged;


    public event Action<
        IReadOnlyList<ColorSignalSymbol>,
        int
    >
        EnteredColorsChanged;


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField]
    private ColorMemoryMissionManager
        colorMemoryMissionManager;


    // ============================================================
    // Input
    // ============================================================

    [Header("Input")]

    [SerializeField, Tooltip(
        "入力開始時にButton2～4のどれかが押されていた場合、" +
        "一度すべて離すまで入力しない")]
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
    // Internal
    // ============================================================

    private readonly List<ColorSignalSymbol>
        enteredColors =
            new List<ColorSignalSymbol>();


    private bool inputEnabled =
        false;


    private bool waitingForInitialRelease =
        false;


    private int previousButton2 =
        0;

    private int previousButton3 =
        0;

    private int previousButton4 =
        0;


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
            colorMemoryMissionManager !=
            null
        )
        {
            HandleMissionStateChanged(
                colorMemoryMissionManager
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

        ResetInputState();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (
            !inputEnabled ||
            colorMemoryMissionManager ==
            null
        )
        {
            SyncButtonState();

            return;
        }


        if (
            colorMemoryMissionManager
                .GetCurrentState()
            !=
            ColorMemoryMissionManager
                .MissionState
                .Inputting
        )
        {
            SyncButtonState();

            return;
        }


        if (
            Time.timeScale <=
            Mathf.Epsilon
        )
        {
            SyncButtonState();

            return;
        }


        int currentButton2 =
            DataManager
                .GetSensorButton2();


        int currentButton3 =
            DataManager
                .GetSensorButton3();


        int currentButton4 =
            DataManager
                .GetSensorButton4();


        // ========================================================
        // 最初の全ボタン解放待ち
        // ========================================================

        if (waitingForInitialRelease)
        {
            bool allReleased =
                currentButton2 == 0 &&
                currentButton3 == 0 &&
                currentButton4 == 0;


            previousButton2 =
                currentButton2;

            previousButton3 =
                currentButton3;

            previousButton4 =
                currentButton4;


            if (allReleased)
            {
                waitingForInitialRelease =
                    false;
            }


            return;
        }


        // ========================================================
        // 0 → 1
        // ========================================================

        bool button2Pressed =
            currentButton2 == 1 &&
            previousButton2 != 1;


        bool button3Pressed =
            currentButton3 == 1 &&
            previousButton3 != 1;


        bool button4Pressed =
            currentButton4 == 1 &&
            previousButton4 != 1;


        // ========================================================
        // 色
        // ========================================================

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


        previousButton2 =
            currentButton2;

        previousButton3 =
            currentButton3;

        previousButton4 =
            currentButton4;
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<
                    ColorMemoryMissionManager
                >();
        }
    }


    // ============================================================
    // Events
    // ============================================================

    private void SubscribeEvents()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        colorMemoryMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;


        colorMemoryMissionManager
            .MissionStateChanged +=
                HandleMissionStateChanged;
    }


    private void UnsubscribeEvents()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        colorMemoryMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;
    }


    // ============================================================
    // Mission State
    // ============================================================

    private void HandleMissionStateChanged(
        ColorMemoryMissionManager
            .MissionState newState
    )
    {
        if (
            newState ==
            ColorMemoryMissionManager
                .MissionState
                .Inputting
        )
        {
            BeginInputMode();

            return;
        }


        EndInputMode();
    }


    // ============================================================
    // Begin
    // ============================================================

    private void BeginInputMode()
    {
        enteredColors.Clear();


        inputEnabled =
            true;


        int currentButton2 =
            DataManager
                .GetSensorButton2();


        int currentButton3 =
            DataManager
                .GetSensorButton3();


        int currentButton4 =
            DataManager
                .GetSensorButton4();


        previousButton2 =
            currentButton2;

        previousButton3 =
            currentButton3;

        previousButton4 =
            currentButton4;


        waitingForInitialRelease =
            requireReleaseBeforeFirstInput &&
            (
                currentButton2 == 1 ||
                currentButton3 == 1 ||
                currentButton4 == 1
            );


        InputModeChanged?.Invoke(
            true
        );


        NotifyEnteredColorsChanged();


        if (debugLog)
        {
            Debug.Log(
                "色入力受付開始。必要色数=" +
                GetExpectedColorCount()
            );
        }
    }


    // ============================================================
    // End
    // ============================================================

    private void EndInputMode()
    {
        bool wasInputEnabled =
            inputEnabled;


        inputEnabled =
            false;


        waitingForInitialRelease =
            false;


        SyncButtonState();


        if (wasInputEnabled)
        {
            InputModeChanged?.Invoke(
                false
            );
        }
    }


    // ============================================================
    // Register
    // ============================================================

    private void RegisterColor(
        ColorSignalSymbol color
    )
    {
        if (!inputEnabled)
        {
            return;
        }


        int expectedCount =
            GetExpectedColorCount();


        if (
            expectedCount <= 0 ||
            enteredColors.Count >=
            expectedCount
        )
        {
            return;
        }


        enteredColors.Add(
            color
        );


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
            enteredColors.Count >=
            expectedCount
        )
        {
            CompleteInput();
        }
    }


    // ============================================================
    // Complete
    // ============================================================

    private void CompleteInput()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
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


    // ============================================================
    // Sync
    // ============================================================

    private void SyncButtonState()
    {
        previousButton2 =
            DataManager
                .GetSensorButton2();


        previousButton3 =
            DataManager
                .GetSensorButton3();


        previousButton4 =
            DataManager
                .GetSensorButton4();
    }


    // ============================================================
    // Notify
    // ============================================================

    private void NotifyEnteredColorsChanged()
    {
        EnteredColorsChanged?.Invoke(
            enteredColors,
            GetExpectedColorCount()
        );
    }


    // ============================================================
    // Getter
    // ============================================================

    private int GetExpectedColorCount()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
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
        return
            enteredColors;
    }


    public int GetExpectedColorCountForDisplay()
    {
        return
            GetExpectedColorCount();
    }


    public bool GetIsInputEnabled()
    {
        return
            inputEnabled;
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


    // ============================================================
    // Reset
    // ============================================================

    private void ResetInputState()
    {
        inputEnabled =
            false;


        waitingForInitialRelease =
            false;


        previousButton2 =
            0;

        previousButton3 =
            0;

        previousButton4 =
            0;
    }
}