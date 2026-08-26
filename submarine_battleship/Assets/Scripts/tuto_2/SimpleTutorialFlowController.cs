using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class SimpleTutorialFlowController : MonoBehaviour
{
    // ============================================================
    // State
    // ============================================================

    private enum SimpleTutorialState
    {
        None = 0,

        Introduction = 1,

        WaitForFirstLowering = 2,
        WaitForSonarOpen = 3,
        WaitForSonarClose = 4,

        WaitForRaising = 5,
        WaitForEnemyFound = 6,

        WaitForSignalStart = 7,
        WaitForSignalFinished = 8,

        WaitForSecondLowering = 9,
        WaitForInputReady = 10,
        WaitForSignalInput = 11,
        WaitForTransmission = 12,
        WaitForEvaluation = 13,
        WaitForResult = 14,

        WaitForRetryRaising = 15,
        WaitForRetrySignalReady = 16,

        WaitForGameStart = 17,

        Completed = 18
    }


    // ============================================================
    // Constants
    // ============================================================

    private const string DefaultMainSceneName =
        "MainScene";

    private const int TotalTutorialSteps =
        5;

    private const float DefaultResultWaitDuration =
        1.5f;

    private const float DefaultRaisedHeightTolerance =
        0.05f;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "簡略版チュートリアルで使用するMissionPanel")]
    private GameObject missionPanel;


    [SerializeField, Tooltip(
        "MissionPanel内のTextMeshPro")]
    private TMP_Text missionText;


    // ============================================================
    // Tutorial Systems
    // ============================================================

    [Header("Tutorial Systems")]

    [SerializeField]
    private TutorialSensorBridgeV2 sensorBridge;


    [SerializeField]
    private TutorialSonarControllerV2 sonarController;


    [SerializeField]
    private TutorialEnemyControllerV2 tutorialEnemy;


    // ============================================================
    // Existing Game Systems
    // ============================================================

    [Header("Existing Game Systems")]

    [SerializeField]
    private CommunicationMissionManager
        communicationMissionManager;


    [SerializeField, Tooltip(
        "上下移動するPeriscopeRoot")]
    private Transform periscopeTransform;


    // ============================================================
    // Scene
    // ============================================================

    [Header("Scene")]

    [SerializeField]
    private string mainSceneName =
        DefaultMainSceneName;


    // ============================================================
    // Settings
    // ============================================================

    [Header("Settings")]

    [SerializeField, Min(0.0f)]
    private float resultWaitDuration =
        DefaultResultWaitDuration;


    [SerializeField, Tooltip(
        "SimpleTutorialScene開始時の潜望鏡高さを" +
        "完全上昇位置として使用する")]
    private bool useInitialHeightAsRaisedPosition =
        true;


    [SerializeField, Min(0.0f)]
    private float raisedHeightTolerance =
        DefaultRaisedHeightTolerance;


    // ============================================================
    // Text
    // ============================================================

    [Header("Introduction")]

    [SerializeField, TextArea(8, 15)]
    private string introductionText =
        "ゲームでは4つのボタンと潜望鏡を使用します。\n\n" +
        "黒いボタン：ソナー\n" +
        "赤いボタン：潜望鏡を上げる\n" +
        "青いボタン：潜望鏡を下げる\n" +
        "黄色いボタン：信号入力\n\n" +
        "黒いボタンまたはEnterキーで開始してください。";


    [Header("Step 1 - Sonar")]

    [SerializeField, TextArea(4, 10)]
    private string firstLoweringText =
        "ソナーを使用するため、\n" +
        "青いボタンを押し続けて\n" +
        "潜望鏡を完全に格納してください。";


    [SerializeField, TextArea(4, 10)]
    private string sonarOpenText =
        "潜望鏡を格納している間は\n" +
        "ソナーを使用できます。\n\n" +
        "黒いボタンを押し続けて\n" +
        "周囲の船の位置を確認してください。";


    [SerializeField, TextArea(4, 10)]
    private string sonarCloseText =
        "ソナーに表示された船の位置を確認してください。\n\n" +
        "確認できたら黒いボタンを離してください。";


    [Header("Step 2 - Periscope")]

    [SerializeField, TextArea(4, 10)]
    private string raisingText =
        "赤いボタンを押し続けて\n" +
        "潜望鏡を海上まで上げてください。";


    [SerializeField, TextArea(4, 10)]
    private string enemySearchText =
        "ソナーで確認した方向を参考に、\n" +
        "潜望鏡を回して敵船を探してください。\n\n" +
        "ソナーだけでは船の種類を判別できません。";


    [Header("Step 3 - Signal")]

    [SerializeField, TextArea(8, 15)]
    private string signalExplanationText =
        "敵船は光で信号を送ります。\n\n" +
        "黄色：信号開始の合図\n" +
        "赤色：短信号「・」\n" +
        "オレンジ色：長信号「―」\n\n" +
        "黄色の開始合図は入力しません。\n" +
        "赤とオレンジの順番を記憶してください。\n\n" +
        "黒いボタンまたはEnterキーで信号を開始します。";


    [SerializeField, TextArea(4, 10)]
    private string signalObservationText =
        "敵船の光信号を観察し、\n" +
        "赤とオレンジの順番を記憶してください。";


    [Header("Step 4 - Communication")]

    [SerializeField, TextArea(4, 10)]
    private string secondLoweringText =
        "信号を記憶したら、\n" +
        "青いボタンを押し続けて\n" +
        "潜望鏡を完全に格納してください。";


    [SerializeField, TextArea(5, 10)]
    private string signalInputText =
        "記憶した信号を黄色いボタンで入力してください。\n\n" +
        "短く押す = ・\n" +
        "長く押す = ―";


    [SerializeField, TextArea(3, 8)]
    private string transmittingText =
        "信号を送信しています。\n" +
        "操作せずお待ちください。";


    [SerializeField, TextArea(3, 8)]
    private string evaluatingText =
        "送信した信号を照合しています。\n" +
        "操作せずお待ちください。";


    [SerializeField, TextArea(4, 10)]
    private string retryRaisingText =
        "入力した信号が一致しませんでした。\n\n" +
        "赤いボタンを押し続けて潜望鏡を上げ、\n" +
        "もう一度信号を確認してください。";


    [SerializeField, TextArea(4, 10)]
    private string retrySignalText =
        "敵船の信号をもう一度確認してください。\n\n" +
        "赤とオレンジの順番を記憶してください。";


    [Header("Step 5 - Exposure Risk")]

    [SerializeField, TextArea(8, 16)]
    private string riskExplanationText =
        "潜望鏡や通信マストを海上に出している間は、\n" +
        "危険度が上昇します。\n\n" +
        "潜望鏡と通信マストを収納している間は、\n" +
        "危険度が低下します。\n\n" +
        "危険度が限界に達すると敵に発見され、\n" +
        "スコアを失います。\n\n" +
        "必要な時だけ潜望鏡を使用することが重要です。\n\n" +
        "黒いボタンまたはEnterキーでゲームを開始します。";


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

    private SimpleTutorialState currentState =
        SimpleTutorialState.None;


    private int previousButton1 =
        0;


    private float initialRaisedLocalY =
        0.0f;


    private bool isChangingScene =
        false;


    private Coroutine resultCoroutine;

    private Coroutine retrySignalCoroutine;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        Time.timeScale =
            1.0f;


        // ========================================================
        // MainScene用MenuPanelManagerを停止
        // ========================================================

        MenuPanelManager mainMenuManager =
            FindFirstObjectByType<MenuPanelManager>();


        if (mainMenuManager != null)
        {
            mainMenuManager.enabled =
                false;
        }
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();


        if (periscopeTransform != null)
        {
            initialRaisedLocalY =
                periscopeTransform
                    .localPosition
                    .y;
        }


        SubscribeEvents();


        if (sonarController != null)
        {
            sonarController.SetInputEnabled(
                false
            );
        }


        if (tutorialEnemy != null)
        {
            tutorialEnemy.SetDetectionEnabled(
                false
            );
        }


        SetAllButtonsDisabled();


        ShowIntroduction();
    }


    // ============================================================
    // OnDestroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeEvents();


        if (resultCoroutine != null)
        {
            StopCoroutine(
                resultCoroutine
            );


            resultCoroutine =
                null;
        }


        if (retrySignalCoroutine != null)
        {
            StopCoroutine(
                retrySignalCoroutine
            );


            retrySignalCoroutine =
                null;
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (isChangingScene)
        {
            return;
        }


        switch (currentState)
        {
            // ====================================================
            // Introduction
            // ====================================================

            case SimpleTutorialState.Introduction:

                if (ReadAdvanceInput())
                {
                    BeginFirstLowering();
                }

                break;


            // ====================================================
            // Step 1
            // ====================================================

            case SimpleTutorialState.WaitForFirstLowering:

                CheckFirstLowering();

                break;


            case SimpleTutorialState.WaitForSonarOpen:

                CheckSonarOpen();

                break;


            case SimpleTutorialState.WaitForSonarClose:

                CheckSonarClose();

                break;


            // ====================================================
            // Step 2
            // ====================================================

            case SimpleTutorialState.WaitForRaising:

                CheckRaising(
                    false
                );

                break;


            // ====================================================
            // Step 3
            // ====================================================

            case SimpleTutorialState.WaitForSignalStart:

                if (ReadAdvanceInput())
                {
                    BeginSignalObservation();
                }

                break;


            // ====================================================
            // Step 4
            // ====================================================

            case SimpleTutorialState.WaitForSecondLowering:

                CheckSecondLowering();

                break;


            case SimpleTutorialState.WaitForRetryRaising:

                CheckRaising(
                    true
                );

                break;


            // ====================================================
            // Step 5
            // ====================================================

            case SimpleTutorialState.WaitForGameStart:

                if (ReadAdvanceInput())
                {
                    ChangeToMainScene();
                }

                break;
        }
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (sensorBridge == null)
        {
            sensorBridge =
                FindFirstObjectByType<
                    TutorialSensorBridgeV2
                >();
        }


        if (sonarController == null)
        {
            sonarController =
                FindFirstObjectByType<
                    TutorialSonarControllerV2
                >();
        }


        if (tutorialEnemy == null)
        {
            tutorialEnemy =
                FindFirstObjectByType<
                    TutorialEnemyControllerV2
                >();
        }


        if (communicationMissionManager == null)
        {
            communicationMissionManager =
                FindFirstObjectByType<
                    CommunicationMissionManager
                >();
        }


        if (periscopeTransform == null)
        {
            PeriscopeController periscopeController =
                FindFirstObjectByType<
                    PeriscopeController
                >();


            if (periscopeController != null)
            {
                periscopeTransform =
                    periscopeController.transform;
            }
        }
    }


    // ============================================================
    // Events
    // ============================================================

    private void SubscribeEvents()
    {
        if (tutorialEnemy != null)
        {
            tutorialEnemy.EnemyFound -=
                HandleEnemyFound;


            tutorialEnemy.EnemyFound +=
                HandleEnemyFound;


            tutorialEnemy.SignalFinished -=
                HandleSignalFinished;


            tutorialEnemy.SignalFinished +=
                HandleSignalFinished;
        }


        if (communicationMissionManager != null)
        {
            communicationMissionManager
                .MissionStateChanged -=
                    HandleMissionStateChanged;


            communicationMissionManager
                .MissionStateChanged +=
                    HandleMissionStateChanged;


            communicationMissionManager
                .MissionEvaluated -=
                    HandleMissionEvaluated;


            communicationMissionManager
                .MissionEvaluated +=
                    HandleMissionEvaluated;
        }
    }


    private void UnsubscribeEvents()
    {
        if (tutorialEnemy != null)
        {
            tutorialEnemy.EnemyFound -=
                HandleEnemyFound;


            tutorialEnemy.SignalFinished -=
                HandleSignalFinished;
        }


        if (communicationMissionManager != null)
        {
            communicationMissionManager
                .MissionStateChanged -=
                    HandleMissionStateChanged;


            communicationMissionManager
                .MissionEvaluated -=
                    HandleMissionEvaluated;
        }
    }


    // ============================================================
    // Introduction
    // ============================================================

    private void ShowIntroduction()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton1Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState.Introduction;


        previousButton1 =
            DataManager.GetSensorButton1();


        ShowText(
            "QUICK TUTORIAL",
            introductionText
        );
    }


    // ============================================================
    // Step 1 / 5 - Sonar
    // ============================================================

    private void BeginFirstLowering()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton3Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForFirstLowering;


        ShowStep(
            1,
            "ソナー",
            firstLoweringText
        );
    }


    private void CheckFirstLowering()
    {
        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton1Allowed(
                true
            );
        }


        if (sonarController != null)
        {
            sonarController.SetInputEnabled(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForSonarOpen;


        ShowStep(
            1,
            "ソナー",
            sonarOpenText
        );


        DebugMessage(
            "Step 1: 潜望鏡格納完了"
        );
    }


    private void CheckSonarOpen()
    {
        if (
            sonarController == null ||
            !sonarController
                .GetIsSonarPanelOpen()
        )
        {
            return;
        }


        currentState =
            SimpleTutorialState
                .WaitForSonarClose;


        ShowStep(
            1,
            "ソナー",
            sonarCloseText
        );
    }


    private void CheckSonarClose()
    {
        if (
            sonarController == null ||
            sonarController
                .GetIsSonarPanelOpen()
        )
        {
            return;
        }


        sonarController.SetInputEnabled(
            false
        );


        SetAllButtonsDisabled();


        BeginRaising();
    }


    // ============================================================
    // Step 2 / 5 - Periscope
    // ============================================================

    private void BeginRaising()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton2Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForRaising;


        ShowStep(
            2,
            "潜望鏡",
            raisingText
        );
    }


    private void CheckRaising(
        bool retry
    )
    {
        if (!IsPeriscopeRaised())
        {
            return;
        }


        SetAllButtonsDisabled();


        if (retry)
        {
            currentState =
                SimpleTutorialState
                    .WaitForRetrySignalReady;


            retrySignalCoroutine =
                StartCoroutine(
                    BeginRetrySignalWhenReady()
                );


            return;
        }


        BeginEnemySearch();
    }


    private bool IsPeriscopeRaised()
    {
        if (
            useInitialHeightAsRaisedPosition &&
            periscopeTransform != null
        )
        {
            float currentY =
                periscopeTransform
                    .localPosition
                    .y;


            return
                currentY >=
                initialRaisedLocalY -
                raisedHeightTolerance;
        }


        return
            DataManager
                .GetIsPeriscopeAboveSurface();
    }


    private void BeginEnemySearch()
    {
        currentState =
            SimpleTutorialState
                .WaitForEnemyFound;


        ShowStep(
            2,
            "潜望鏡",
            enemySearchText
        );


        if (tutorialEnemy != null)
        {
            tutorialEnemy.SetDetectionEnabled(
                true
            );
        }


        DebugMessage(
            "Step 2: 敵船探索開始"
        );
    }


    private void HandleEnemyFound()
    {
        if (
            currentState !=
            SimpleTutorialState
                .WaitForEnemyFound
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        ShowSignalExplanation();
    }


    // ============================================================
    // Step 3 / 5 - Signal
    // ============================================================

    private void ShowSignalExplanation()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton1Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForSignalStart;


        previousButton1 =
            DataManager.GetSensorButton1();


        ShowStep(
            3,
            "光信号",
            signalExplanationText
        );


        DebugMessage(
            "Step 3: 信号説明"
        );
    }


    private void BeginSignalObservation()
    {
        SetAllButtonsDisabled();


        currentState =
            SimpleTutorialState
                .WaitForSignalFinished;


        ShowStep(
            3,
            "光信号",
            signalObservationText
        );


        if (
            tutorialEnemy == null ||
            !tutorialEnemy
                .BeginSignalMission()
        )
        {
            Debug.LogError(
                "SimpleTutorial: " +
                "敵船の信号を開始できませんでした。"
            );
        }
    }


    private void HandleSignalFinished()
    {
        if (
            currentState !=
            SimpleTutorialState
                .WaitForSignalFinished
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        BeginSecondLowering();
    }


    // ============================================================
    // Step 4 / 5 - Communication
    // ============================================================

    private void BeginSecondLowering()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton3Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForSecondLowering;


        ShowStep(
            4,
            "通信",
            secondLoweringText
        );
    }


    private void CheckSecondLowering()
    {
        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        currentState =
            SimpleTutorialState
                .WaitForInputReady;


        ShowStep(
            4,
            "通信",
            "通信入力を準備しています。"
        );


        // CommunicationMissionManagerがInputtingへ
        // 移行するとイベントからBeginSignalInput()へ進む。
    }


    // ============================================================
    // Communication Mission State
    // ============================================================

    private void HandleMissionStateChanged(
        CommunicationMissionManager
            .MissionState state
    )
    {
        switch (state)
        {
            // ====================================================
            // Inputting
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Inputting:

                if (
                    currentState ==
                        SimpleTutorialState
                            .WaitForSecondLowering
                    ||
                    currentState ==
                        SimpleTutorialState
                            .WaitForInputReady
                )
                {
                    BeginSignalInput();
                }

                break;


            // ====================================================
            // Transmitting
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Transmitting:

                if (
                    currentState ==
                    SimpleTutorialState
                        .WaitForSignalInput
                )
                {
                    BeginTransmissionWait();
                }

                break;


            // ====================================================
            // Evaluating
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Evaluating:

                if (
                    currentState ==
                        SimpleTutorialState
                            .WaitForTransmission
                    ||
                    currentState ==
                        SimpleTutorialState
                            .WaitForSignalInput
                )
                {
                    BeginEvaluationWait();
                }

                break;
        }
    }


    private void BeginSignalInput()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton4Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForSignalInput;


        ShowStep(
            4,
            "通信",
            signalInputText
        );


        DebugMessage(
            "Step 4: 信号入力開始"
        );
    }


    private void BeginTransmissionWait()
    {
        SetAllButtonsDisabled();


        currentState =
            SimpleTutorialState
                .WaitForTransmission;


        ShowStep(
            4,
            "通信",
            transmittingText
        );
    }


    private void BeginEvaluationWait()
    {
        SetAllButtonsDisabled();


        currentState =
            SimpleTutorialState
                .WaitForEvaluation;


        ShowStep(
            4,
            "通信",
            evaluatingText
        );
    }


    // ============================================================
    // Communication Result
    // ============================================================

    private void HandleMissionEvaluated(
        bool successful
    )
    {
        SetAllButtonsDisabled();


        currentState =
            SimpleTutorialState
                .WaitForResult;


        if (resultCoroutine != null)
        {
            StopCoroutine(
                resultCoroutine
            );
        }


        resultCoroutine =
            StartCoroutine(
                successful
                    ? SuccessRoutine()
                    : FailureRoutine()
            );
    }


    private IEnumerator SuccessRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                resultWaitDuration
            );


        resultCoroutine =
            null;


        BeginRiskExplanation();
    }


    private IEnumerator FailureRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                resultWaitDuration
            );


        resultCoroutine =
            null;


        BeginRetryRaising();
    }


    // ============================================================
    // Retry
    // ============================================================

    private void BeginRetryRaising()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton2Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForRetryRaising;


        ShowStep(
            4,
            "通信",
            retryRaisingText
        );
    }


    private IEnumerator BeginRetrySignalWhenReady()
    {
        while (
            communicationMissionManager != null &&
            communicationMissionManager
                .GetCurrentState()
            !=
            CommunicationMissionManager
                .MissionState
                .Searching
        )
        {
            yield return
                null;
        }


        retrySignalCoroutine =
            null;


        currentState =
            SimpleTutorialState
                .WaitForSignalFinished;


        ShowStep(
            3,
            "光信号",
            retrySignalText
        );


        if (
            tutorialEnemy == null ||
            !tutorialEnemy
                .BeginSignalMission()
        )
        {
            Debug.LogError(
                "SimpleTutorial: " +
                "再試行用の信号を開始できませんでした。"
            );
        }
    }


    // ============================================================
    // Step 5 / 5 - Exposure Risk
    // ============================================================

    private void BeginRiskExplanation()
    {
        SetAllButtonsDisabled();


        // ========================================================
        // 危険度は簡略版では実演しない。
        // 説明だけ行う。
        // ========================================================

        if (sensorBridge != null)
        {
            sensorBridge.SetButton1Allowed(
                true
            );
        }


        currentState =
            SimpleTutorialState
                .WaitForGameStart;


        previousButton1 =
            DataManager.GetSensorButton1();


        ShowStep(
            5,
            "危険度",
            riskExplanationText
        );


        DebugMessage(
            "Step 5: 危険度説明"
        );
    }


    // ============================================================
    // MainScene
    // ============================================================

    private void ChangeToMainScene()
    {
        if (isChangingScene)
        {
            return;
        }


        isChangingScene =
            true;


        SetAllButtonsDisabled();


        if (sonarController != null)
        {
            sonarController.SetInputEnabled(
                false
            );
        }


        currentState =
            SimpleTutorialState.Completed;


        Time.timeScale =
            1.0f;


        SceneManager.LoadScene(
            mainSceneName
        );
    }


    // ============================================================
    // Input Control
    // ============================================================

    private void SetAllButtonsDisabled()
    {
        if (sensorBridge != null)
        {
            sensorBridge
                .SetAllGameplayButtonsAllowed(
                    false
                );
        }
    }


    // ============================================================
    // Advance Input
    // ============================================================

    private bool ReadAdvanceInput()
    {
        bool pressed =
            false;


        // ========================================================
        // 黒いボタン
        // ========================================================

        int currentButton1 =
            DataManager.GetSensorButton1();


        if (
            currentButton1 == 1 &&
            previousButton1 != 1
        )
        {
            pressed =
                true;
        }


        previousButton1 =
            currentButton1;


        // ========================================================
        // Keyboard
        // ========================================================

        if (Keyboard.current != null)
        {
            if (
                Keyboard.current
                    .enterKey
                    .wasPressedThisFrame
                ||
                Keyboard.current
                    .numpadEnterKey
                    .wasPressedThisFrame
            )
            {
                pressed =
                    true;
            }
        }


        // ========================================================
        // Gamepad
        // ========================================================

        Gamepad gamepad =
            Gamepad.current;


        if (
            gamepad != null &&
            gamepad
                .buttonEast
                .wasPressedThisFrame
        )
        {
            pressed =
                true;
        }


        return
            pressed;
    }


    // ============================================================
    // UI
    // ============================================================

    private void ShowStep(
        int step,
        string title,
        string body
    )
    {
        string header =
            "QUICK TUTORIAL " +
            step +
            " / " +
            TotalTutorialSteps;


        ShowText(
            header +
            "\n" +
            title,
            body
        );
    }


    private void ShowText(
        string header,
        string body
    )
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(
                true
            );
        }


        if (missionText == null)
        {
            return;
        }


        missionText.text =
            header +
            "\n\n" +
            body;
    }


    // ============================================================
    // Debug
    // ============================================================

    private void DebugMessage(
        string message
    )
    {
        if (!debugLog)
        {
            return;
        }


        Debug.Log(
            "SimpleTutorial: " +
            message
        );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        resultWaitDuration =
            Mathf.Max(
                0.0f,
                resultWaitDuration
            );


        raisedHeightTolerance =
            Mathf.Max(
                0.0f,
                raisedHeightTolerance
            );


        if (
            string.IsNullOrWhiteSpace(
                mainSceneName
            )
        )
        {
            mainSceneName =
                DefaultMainSceneName;
        }
    }
}