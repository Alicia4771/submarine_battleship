using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class ColorTutorialFlowController : MonoBehaviour
{
    // ============================================================
    // Tutorial State
    // ============================================================

    private enum TutorialState
    {
        None = 0,
        WaitForSonarOpen = 1,
        WaitForSonarRelease = 2,
        WaitForPeriscopeRaised = 3,
        WaitForEnemyInView = 4,
        PlayingSignal = 5,
        WaitForPeriscopeLowered = 6,
        WaitForInputReady = 7,
        WaitForMissionResult = 8,
        ShowingResetHint = 9,
        WaitForDangerPeriscopeExposure = 10,
        WaitForDangerDetection = 11,
        ShowingDetectionResult = 12,
        Completed = 13
    }


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField]
    private ColorTutorialSensorBridge sensorBridge;


    [SerializeField, Tooltip(
        "緑ボタン（Button5）の強制スキップ判定に使用するSensorRead。" +
        "未設定の場合は自動検索する")]
    private SensorRead sensorRead;


    [SerializeField]
    private ColorTutorialVideoController videoController;


    [SerializeField]
    private ColorTutorialEnemySpawner enemySpawner;


    [SerializeField]
    private ColorMemoryMissionManager colorMemoryMissionManager;


    [SerializeField, Tooltip(
        "チュートリアル最後の危険度デモで使用するExposureRiskManager")]
    private ExposureRiskManager exposureRiskManager;


    [SerializeField, Tooltip(
        "潜望鏡表示に使用しているCamera")]
    private Camera periscopeCamera;


    [SerializeField]
    private GameObject sonarPanel;


    [SerializeField]
    private TMP_Text stepText;


    [SerializeField]
    private TMP_Text instructionText;


    // ============================================================
    // Step 1
    // ============================================================

    [Header("Step 1 - Sonar")]

    [SerializeField, TextArea(2, 4)]
    private string sonarInstruction =
        "黒いボタンを押して\nソナーを表示してください。";


    [SerializeField, TextArea(2, 4)]
    private string sonarReleaseInstruction =
        "ソナーで周囲を確認したら、\n黒いボタンを離してください。";


    // ============================================================
    // Step 2
    // ============================================================

    [Header("Step 2 - Raise Periscope")]

    [SerializeField, TextArea(2, 4)]
    private string raiseInstruction =
        "赤いボタンを押し続けて\n潜望鏡を海面上まで上げてください。";


    // ============================================================
    // Step 3
    // ============================================================

    [Header("Step 3 - Find Enemy")]

    [SerializeField, TextArea(2, 4)]
    private string findEnemyInstruction =
        "潜望鏡を左へ回して\n敵艦を視界にとらえてください。";


    [SerializeField, Range(0.0f, 0.45f)]
    private float enemyViewportMargin =
        0.03f;


    [SerializeField, Min(0.0f)]
    private float requiredEnemyVisibleDuration =
        0.5f;


    [SerializeField]
    private bool requirePeriscopeAboveSurface =
        true;


    // ============================================================
    // Step 4
    // ============================================================

    [Header("Step 4 - Memorize Signal")]

    [SerializeField, TextArea(2, 5)]
    private string memorizeInstruction =
        "敵艦の光信号を確認してください。\n白い光は開始の合図です。\n続く4色の順番を覚えてください。";


    [SerializeField, Min(0.0f), Tooltip(
        "STEP4表示後、発光を開始するまでの待ち時間")]
    private float signalStartDelay =
        0.8f;


    [SerializeField, Min(1), Tooltip(
        "この回数だけ信号を最後まで見たらSTEP5へ進める。" +
        "STEP5へ進んだ後も、潜望鏡を完全格納するまでは信号は繰り返し続ける")]
    private int minimumSignalCyclesBeforeLowering =
        1;


    // ============================================================
    // Step 5
    // ============================================================

    [Header("Step 5 - Lower Periscope")]

    [SerializeField, TextArea(2, 4)]
    private string lowerInstruction =
        "青いボタンを押し続けて\n潜望鏡を完全に格納してください。";


    // ============================================================
    // Step 6
    // ============================================================

    [Header("Step 6 - Input Signal")]

    [SerializeField, TextArea(2, 4)]
    private string inputInstruction =
        "覚えた色を\n順番に入力してください。\n間違えた場合は白いボタンでリセットできます。";


    [SerializeField, TextArea(2, 4)]
    private string retryInputInstruction =
        "入力が違いました。\nもう一度、同じ順番で入力してください。\n白いボタンで入力をリセットできます。";


    [SerializeField, Min(0.0f), Tooltip(
        "失敗表示後、再入力を開始するまでの待ち時間")]
    private float retryDelay =
        1.5f;


    // ============================================================
    // Reset Hint
    // ============================================================

    [Header("Reset Hint")]

    [SerializeField, TextArea(2, 5)]
    private string resetHintInstruction =
        "ヒント\n入力を間違えた場合は、\n白いボタンを押すと入力をリセットできます。";


    [SerializeField, Min(0.0f)]
    private float resultDisplayWait =
        1.5f;


    [SerializeField, Min(0.0f)]
    private float resetHintDuration =
        3.5f;



    // ============================================================
    // Risk Demo
    // ============================================================

    [Header("Risk Demo")]

    [SerializeField, Min(0.0f), Tooltip(
        "危険度デモ開始時の危険度。Detection Thresholdより少し低い値にする")]
    private float riskDemoInitialRisk =
        85.0f;


    [SerializeField, TextArea(3, 6), Tooltip(
        "白ボタンの補足説明後に表示する文章")]
    private string riskDemoInstruction =
        "最後に危険度について確認します。\n危険度が100になると敵に発見され、スコアが減点されます。\n赤いボタンで潜望鏡を海面上まで上げてください。";


    [SerializeField, TextArea(2, 5), Tooltip(
        "潜望鏡が海面上に出た後、発見されるまで表示する文章")]
    private string riskIncreasingInstruction =
        "危険度が上昇しています。\n100になると敵に発見され、スコアが減点されます。";


    [SerializeField, Min(0.0f), Tooltip(
        "敵に発見された後、結果を見せてからメインシーンへ移るまでの時間")]
    private float detectionResultDisplayDuration =
        3.0f;


    [SerializeField, Tooltip(
        "危険度デモ開始時に赤ボタンで潜望鏡を上げる動画を表示する")]
    private bool showRaiseVideoDuringRiskDemo =
        true;


    [SerializeField, Tooltip(
        "発見結果表示後、自動的にMainColorSceneへ移動する")]
    private bool autoLoadMainSceneAfterRiskDemo =
        true;


    // ============================================================
    // Risk Demo
    // ============================================================

    private void BeginRiskDemo()
    {
        DisableAllInputs();


        if (exposureRiskManager == null)
        {
            BeginTutorialComplete();

            return;
        }


        videoController
            .HideVideo();


        // 危険度デモ開始時点のスコアを保存。
        // 発見後に実際に何点減ったかを表示する。
        scoreBeforeRiskDemo =
            DataManager
                .GetScore();


        riskDemoDetectionHandled =
            false;


        // 潜望鏡を上げる前は危険度を固定する。
        // 85を表示したままプレイヤーの操作を待てる。
        exposureRiskManager.enabled =
            true;


        exposureRiskManager
            .SetRiskSystemEnabled(
                false
            );


        exposureRiskManager
            .ResetRisk();


        float detectionThreshold =
            exposureRiskManager
                .GetDetectionThreshold();


        float initialRisk =
            Mathf.Min(
                riskDemoInitialRisk,
                Mathf.Max(
                    0.0f,
                    detectionThreshold -
                    0.1f
                )
            );


        exposureRiskManager
            .SetRisk(
                initialRisk
            );


        sensorBridge
            .AllowOnlyButton2();


        currentState =
            TutorialState
                .WaitForDangerPeriscopeExposure;


        SetTutorialText(
            "危険度",
            riskDemoInstruction
        );


        if (showRaiseVideoDuringRiskDemo)
        {
            videoController
                .PlayRaisePeriscopeVideo();
        }


        DebugMessage(
            "危険度デモ開始 / 初期危険度=" +
            initialRisk
        );
    }


    private void CheckDangerPeriscopeExposure()
    {
        if (
            !DataManager
                .GetIsPeriscopeAboveSurface()
        )
        {
            return;
        }


        // 海面上へ出た瞬間から本物の危険度システムを再開。
        // Periscope Risk Per Secondの設定値で85→100へ自然に上昇する。
        exposureRiskManager
            .SetRiskSystemEnabled(
                true
            );


        currentState =
            TutorialState
                .WaitForDangerDetection;


        videoController
            .HideVideo();


        SetTutorialText(
            "危険度",
            riskIncreasingInstruction
        );


        DebugMessage(
            "潜望鏡露出を確認 / 危険度上昇開始"
        );
    }


    private void HandleEnemyDetectionTriggered(
        int detectionCount
    )
    {
        if (
            currentState !=
            TutorialState.WaitForDangerDetection
            ||
            riskDemoDetectionHandled
        )
        {
            return;
        }


        riskDemoDetectionHandled =
            true;


        DisableAllInputs();


        // ExposureRiskManager側で、
        // このイベントが発生する前にスコア減点と危険度リセットが完了している。
        int scoreAfterDetection =
            DataManager
                .GetScore();


        int scoreDifference =
            scoreAfterDetection -
            scoreBeforeRiskDemo;


        // 発見後の危険度を固定し、結果を落ち着いて見られるようにする。
        exposureRiskManager
            .SetRiskSystemEnabled(
                false
            );


        currentState =
            TutorialState
                .ShowingDetectionResult;


        string scoreDifferenceText =
            scoreDifference >= 0
                ? "+" +
                    scoreDifference
                : scoreDifference
                    .ToString();


        string detectionInstruction =
            "敵に発見されました！\n" +
            "危険度が100になるとスコアが減点されます。\n" +
            "スコア：" +
            scoreBeforeRiskDemo +
            " → " +
            scoreAfterDetection +
            "（" +
            scoreDifferenceText +
            "点）";


        SetTutorialText(
            "危険度",
            detectionInstruction
        );


        StartFlowCoroutine(
            DetectionResultRoutine()
        );


        DebugMessage(
            "敵発見デモ完了 / DetectionCount=" +
            detectionCount +
            " / Score=" +
            scoreBeforeRiskDemo +
            "→" +
            scoreAfterDetection
        );
    }


    private IEnumerator DetectionResultRoutine()
    {
        if (
            detectionResultDisplayDuration >
            0.0f
        )
        {
            yield return
                new WaitForSecondsRealtime(
                    detectionResultDisplayDuration
                );
        }


        flowCoroutine =
            null;


        if (autoLoadMainSceneAfterRiskDemo)
        {
            ChangeToMainColorScene();

            yield break;
        }


        BeginTutorialComplete();
    }


    private void ChangeToMainColorScene()
    {
        if (isChangingScene)
        {
            return;
        }


        isChangingScene =
            true;


        DisableAllInputs();


        Time.timeScale =
            1.0f;


        if (
            string.IsNullOrWhiteSpace(
                mainColorSceneName
            )
        )
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "Main Color Scene Nameが空です。"
            );


            isChangingScene =
                false;

            return;
        }


        SceneManager.LoadScene(
            mainColorSceneName
        );
    }


    // ============================================================
    // Force Skip
    // ============================================================

    [Header("Force Skip")]

    [SerializeField, Tooltip(
        "ONなら、チュートリアル中のどのSTEPでも緑ボタン（Button5）で" +
        "MainColorSceneへ強制遷移できる")]
    private bool enableButton5ForceSkip =
        true;


    [SerializeField, Tooltip(
        "シーン開始時にButton5が押しっぱなしでも誤遷移しないよう、" +
        "一度離された後の押下だけを受け付ける")]
    private bool requireButton5ReleaseBeforeSkip =
        true;


    // ============================================================
    // Complete
    // ============================================================

    [Header("Complete")]

    [SerializeField, TextArea(2, 5)]
    private string completeInstruction =
        "チュートリアル完了！\n黒いボタンを押してゲームを開始してください。";


    [SerializeField]
    private string mainColorSceneName =
        "MainColorScene";


    [SerializeField]
    private bool allowEnterKeyToStart =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    [SerializeField]
    private int debugButton1 =
        0;


    [SerializeField, Tooltip(
        "SensorReadから直接取得した緑ボタン（Button5）の値")]
    private int debugRawButton5 =
        0;


    [SerializeField]
    private bool debugSonarPanelActive =
        false;


    [SerializeField]
    private int debugCurrentStep =
        0;


    [SerializeField]
    private bool debugEnemyInView =
        false;


    [SerializeField]
    private float debugEnemyVisibleTime =
        0.0f;


    [SerializeField]
    private Vector3 debugEnemyViewportPosition =
        Vector3.zero;


    // ============================================================
    // Internal
    // ============================================================

    private TutorialState currentState =
        TutorialState.None;


    private GameObject tutorialEnemy;


    private ColorMemoryEnemyShip tutorialEnemyMemoryComponent;


    private ColorTutorialSignalEmitter tutorialSignalEmitter;


    private float enemyVisibleTime =
        0.0f;


    private int previousDebugButton1 =
        0;


    private bool completionButtonReleased =
        false;


    private bool button5ReleasedAfterSceneStart =
        false;


    private int previousRawButton5 =
        0;


    private bool isChangingScene =
        false;


    private int scoreBeforeRiskDemo =
        0;


    private bool riskDemoDetectionHandled =
        false;


    private Coroutine flowCoroutine;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        Time.timeScale =
            1.0f;


        DisableCompetingSonarControllers();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();


        if (!ValidateReferences())
        {
            enabled =
                false;

            return;
        }


        InitializeButton5ForceSkip();


        SubscribeMissionEvents();


        SubscribeRiskEvents();


        PrepareRiskSystemForTutorial();


        SetSonarPanel(
            false
        );


        sensorBridge
            .SetAllTutorialInputsAllowed(
                false
            );


        BeginStep1Sonar();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeMissionEvents();


        UnsubscribeRiskEvents();


        UnsubscribeSignalEmitter();


        if (flowCoroutine != null)
        {
            StopCoroutine(
                flowCoroutine
            );

            flowCoroutine =
                null;
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        // 緑ボタン（Button5）はチュートリアルのSTEP制御より優先する。
        // SensorBridgeの許可状態に関係なく、SensorReadから直接取得する。
        if (CheckButton5ForceSkip())
        {
            return;
        }


        UpdateDebugValues();


        switch (currentState)
        {
            case TutorialState.WaitForSonarOpen:

                CheckSonarOpen();

                break;


            case TutorialState.WaitForSonarRelease:

                CheckSonarRelease();

                break;


            case TutorialState.WaitForPeriscopeRaised:

                CheckPeriscopeRaised();

                break;


            case TutorialState.WaitForEnemyInView:

                CheckEnemyInView();

                break;


            case TutorialState.WaitForPeriscopeLowered:

                CheckPeriscopeLowered();

                break;


            case TutorialState.WaitForInputReady:

                CheckInputReady();

                break;


            case TutorialState.WaitForDangerPeriscopeExposure:

                CheckDangerPeriscopeExposure();

                break;


            case TutorialState.Completed:

                CheckCompleteInput();

                break;


            case TutorialState.PlayingSignal:
            case TutorialState.WaitForMissionResult:
            case TutorialState.ShowingResetHint:
            case TutorialState.WaitForDangerDetection:
            case TutorialState.ShowingDetectionResult:
            case TutorialState.None:
            default:

                break;
        }
    }


    // ============================================================
    // Force Skip - Button5
    // ============================================================

    private void InitializeButton5ForceSkip()
    {
        int button5 =
            ReadRawButton5();


        previousRawButton5 =
            button5;


        if (requireButton5ReleaseBeforeSkip)
        {
            button5ReleasedAfterSceneStart =
                button5 == 0;
        }
        else
        {
            button5ReleasedAfterSceneStart =
                true;
        }


        debugRawButton5 =
            button5;
    }


    private bool CheckButton5ForceSkip()
    {
        if (
            !enableButton5ForceSkip ||
            isChangingScene ||
            sensorRead == null
        )
        {
            return false;
        }


        int currentButton5 =
            ReadRawButton5();


        debugRawButton5 =
            currentButton5;


        // シーン開始時に押しっぱなしだった場合は、
        // 一度離されるまで強制遷移を受け付けない。
        if (!button5ReleasedAfterSceneStart)
        {
            previousRawButton5 =
                currentButton5;


            if (currentButton5 == 0)
            {
                button5ReleasedAfterSceneStart =
                    true;
            }


            return false;
        }


        bool pressedThisFrame =
            currentButton5 == 1 &&
            previousRawButton5 != 1;


        previousRawButton5 =
            currentButton5;


        if (!pressedThisFrame)
        {
            return false;
        }


        DebugMessage(
            "Button5（緑）を検出したため、チュートリアルをスキップします。"
        );


        ChangeToMainColorScene();


        return true;
    }


    private int ReadRawButton5()
    {
        if (sensorRead == null)
        {
            return 0;
        }


        sensorRead.GetSensorData(
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out int button5,
            out _
        );


        return
            button5;
    }


    // ============================================================
    // Competing Controllers
    // ============================================================

    private void DisableCompetingSonarControllers()
    {
        MenuPanelManager[] menuManagers =
            FindObjectsByType<MenuPanelManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        for (
            int index = 0;
            index < menuManagers.Length;
            index++
        )
        {
            MenuPanelManager manager =
                menuManagers[index];


            if (manager == null)
            {
                continue;
            }


            manager.CloseSonarPanel();


            manager.enabled =
                false;
        }


        TutorialSonarControllerV2[] tutorialSonarControllers =
            FindObjectsByType<TutorialSonarControllerV2>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        for (
            int index = 0;
            index < tutorialSonarControllers.Length;
            index++
        )
        {
            TutorialSonarControllerV2 controller =
                tutorialSonarControllers[index];


            if (controller == null)
            {
                continue;
            }


            controller.SetInputEnabled(
                false
            );


            controller.enabled =
                false;
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
                    ColorTutorialSensorBridge
                >();
        }


        if (sensorRead == null)
        {
            sensorRead =
                FindFirstObjectByType<
                    SensorRead
                >();
        }


        if (videoController == null)
        {
            videoController =
                FindFirstObjectByType<
                    ColorTutorialVideoController
                >();
        }


        if (enemySpawner == null)
        {
            enemySpawner =
                FindFirstObjectByType<
                    ColorTutorialEnemySpawner
                >();
        }


        if (colorMemoryMissionManager == null)
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<
                    ColorMemoryMissionManager
                >();
        }


        if (exposureRiskManager == null)
        {
            exposureRiskManager =
                FindFirstObjectByType<
                    ExposureRiskManager
                >();
        }


        if (periscopeCamera == null)
        {
            periscopeCamera =
                Camera.main;
        }
    }


    private bool ValidateReferences()
    {
        if (sensorBridge == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ColorTutorialSensorBridgeがありません。"
            );

            return false;
        }


        if (
            enableButton5ForceSkip &&
            sensorRead == null
        )
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "Button5強制スキップがONですが、SensorReadがありません。"
            );

            return false;
        }


        if (videoController == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ColorTutorialVideoControllerがありません。"
            );

            return false;
        }


        if (enemySpawner == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ColorTutorialEnemySpawnerがありません。"
            );

            return false;
        }


        if (colorMemoryMissionManager == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ColorMemoryMissionManagerがありません。"
            );

            return false;
        }


        if (exposureRiskManager == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ExposureRiskManagerがありません。"
            );

            return false;
        }


        if (periscopeCamera == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "Periscope Cameraがありません。"
            );

            return false;
        }


        if (sonarPanel == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "SonarPanelがありません。"
            );

            return false;
        }


        return true;
    }


    // ============================================================
    // Mission Events
    // ============================================================

    private void SubscribeMissionEvents()
    {
        if (colorMemoryMissionManager == null)
        {
            return;
        }


        colorMemoryMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;


        colorMemoryMissionManager
            .MissionEvaluated +=
                HandleMissionEvaluated;
    }


    private void UnsubscribeMissionEvents()
    {
        if (colorMemoryMissionManager == null)
        {
            return;
        }


        colorMemoryMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;
    }


    // ============================================================
    // Risk Events
    // ============================================================

    private void SubscribeRiskEvents()
    {
        if (exposureRiskManager == null)
        {
            return;
        }


        exposureRiskManager
            .EnemyDetectionTriggered -=
                HandleEnemyDetectionTriggered;


        exposureRiskManager
            .EnemyDetectionTriggered +=
                HandleEnemyDetectionTriggered;
    }


    private void UnsubscribeRiskEvents()
    {
        if (exposureRiskManager == null)
        {
            return;
        }


        exposureRiskManager
            .EnemyDetectionTriggered -=
                HandleEnemyDetectionTriggered;
    }


    private void PrepareRiskSystemForTutorial()
    {
        if (exposureRiskManager == null)
        {
            return;
        }


        // MonoBehaviour自体は有効のままにしてUIとの接続を維持する。
        exposureRiskManager.enabled =
            true;


        // 通常のチュートリアル進行中は危険度が勝手に上がらないよう停止。
        exposureRiskManager
            .SetRiskSystemEnabled(
                false
            );


        exposureRiskManager
            .ResetRisk();
    }


    // ============================================================
    // Step 1 - Sonar
    // ============================================================

    private void BeginStep1Sonar()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyButton1();


        SetSonarPanel(
            false
        );


        currentState =
            TutorialState.WaitForSonarOpen;


        SetTutorialText(
            "STEP 1 / 6",
            sonarInstruction
        );


        videoController
            .PlaySonarVideo();
    }


    private void CheckSonarOpen()
    {
        if (
            DataManager
                .GetSensorButton1()
            !=
            1
        )
        {
            return;
        }


        SetSonarPanel(
            true
        );


        currentState =
            TutorialState.WaitForSonarRelease;


        SetTutorialText(
            "STEP 1 / 6",
            sonarReleaseInstruction
        );
    }


    private void CheckSonarRelease()
    {
        if (
            DataManager
                .GetSensorButton1()
            ==
            1
        )
        {
            return;
        }


        SetSonarPanel(
            false
        );


        BeginStep2RaisePeriscope();
    }


    // ============================================================
    // Step 2 - Raise
    // ============================================================

    private void BeginStep2RaisePeriscope()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyButton2();


        currentState =
            TutorialState.WaitForPeriscopeRaised;


        SetTutorialText(
            "STEP 2 / 6",
            raiseInstruction
        );


        videoController
            .PlayRaisePeriscopeVideo();
    }


    private void CheckPeriscopeRaised()
    {
        if (
            !DataManager
                .GetIsPeriscopeFullyRaised()
        )
        {
            return;
        }


        BeginStep3FindEnemy();
    }


    // ============================================================
    // Step 3 - Find Enemy
    // ============================================================

    private void BeginStep3FindEnemy()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyYaw();


        if (!ResolveTutorialEnemy())
        {
            return;
        }


        enemyVisibleTime =
            0.0f;


        debugEnemyInView =
            false;


        debugEnemyVisibleTime =
            0.0f;


        currentState =
            TutorialState.WaitForEnemyInView;


        SetTutorialText(
            "STEP 3 / 6",
            findEnemyInstruction
        );


        videoController
            .PlayRotateLeftVideo();
    }


    private void CheckEnemyInView()
    {
        if (!ResolveTutorialEnemy())
        {
            enemyVisibleTime =
                0.0f;

            return;
        }


        bool enemyInView =
            IsEnemyInsidePeriscopeView(
                tutorialEnemy
            );


        debugEnemyInView =
            enemyInView;


        if (!enemyInView)
        {
            enemyVisibleTime =
                0.0f;


            debugEnemyVisibleTime =
                0.0f;


            return;
        }


        enemyVisibleTime +=
            Time.deltaTime;


        debugEnemyVisibleTime =
            enemyVisibleTime;


        if (
            enemyVisibleTime <
            requiredEnemyVisibleDuration
        )
        {
            return;
        }


        BeginStep4MemorizeSignal();
    }


    private bool IsEnemyInsidePeriscopeView(
        GameObject enemy
    )
    {
        if (
            enemy == null ||
            periscopeCamera == null
        )
        {
            return false;
        }


        if (
            requirePeriscopeAboveSurface &&
            !DataManager
                .GetIsPeriscopeAboveSurface()
        )
        {
            return false;
        }


        Vector3 targetPoint =
            GetEnemyViewPoint(
                enemy
            );


        Vector3 viewportPosition =
            periscopeCamera
                .WorldToViewportPoint(
                    targetPoint
                );


        debugEnemyViewportPosition =
            viewportPosition;


        if (viewportPosition.z <= 0.0f)
        {
            return false;
        }


        float minimum =
            enemyViewportMargin;


        float maximum =
            1.0f -
            enemyViewportMargin;


        return
            viewportPosition.x >= minimum &&
            viewportPosition.x <= maximum &&
            viewportPosition.y >= minimum &&
            viewportPosition.y <= maximum;
    }


    private Vector3 GetEnemyViewPoint(
        GameObject enemy
    )
    {
        Renderer[] renderers =
            enemy
                .GetComponentsInChildren<
                    Renderer
                >(
                    true
                );


        bool hasBounds =
            false;


        Bounds combinedBounds =
            new Bounds(
                enemy.transform.position,
                Vector3.zero
            );


        for (
            int index = 0;
            index < renderers.Length;
            index++
        )
        {
            Renderer renderer =
                renderers[index];


            if (
                renderer == null ||
                !renderer.enabled
            )
            {
                continue;
            }


            if (!hasBounds)
            {
                combinedBounds =
                    renderer.bounds;


                hasBounds =
                    true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    renderer.bounds
                );
            }
        }


        return
            hasBounds
                ? combinedBounds.center
                : enemy.transform.position;
    }


    // ============================================================
    // Step 4 - Memorize
    // ============================================================

    private void BeginStep4MemorizeSignal()
    {
        DisableAllInputs();


        videoController
            .HideVideo();


        if (!ResolveTutorialEnemy())
        {
            return;
        }


        tutorialEnemyMemoryComponent =
            tutorialEnemy
                .GetComponent<ColorMemoryEnemyShip>();


        tutorialSignalEmitter =
            tutorialEnemy
                .GetComponent<
                    ColorTutorialSignalEmitter
                >();


        if (tutorialEnemyMemoryComponent == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "TutorialEnemyにColorMemoryEnemyShipがありません。"
            );

            return;
        }


        if (tutorialSignalEmitter == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "TutorialEnemyにColorTutorialSignalEmitterがありません。"
            );

            return;
        }


        UnsubscribeSignalEmitter();


        tutorialSignalEmitter
            .SequenceCycleCompleted +=
                HandleTutorialSignalCycleCompleted;


        tutorialSignalEmitter
            .SignalFinished +=
                HandleTutorialSignalFinished;


        bool missionStarted =
            colorMemoryMissionManager
                .TryBeginMission(
                    tutorialEnemyMemoryComponent,
                    tutorialSignalEmitter
                        .GetFixedSequence()
                );


        if (!missionStarted)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "色記憶ミッションを開始できませんでした。"
            );

            return;
        }


        currentState =
            TutorialState.PlayingSignal;


        SetTutorialText(
            "STEP 4 / 6",
            memorizeInstruction
        );


        StartFlowCoroutine(
            StartSignalAfterDelay()
        );
    }


    private IEnumerator StartSignalAfterDelay()
    {
        if (signalStartDelay > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    signalStartDelay
                );
        }


        if (
            tutorialSignalEmitter == null ||
            !tutorialSignalEmitter
                .PlaySignal()
        )
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "固定色信号を開始できませんでした。"
            );
        }


        flowCoroutine =
            null;
    }


    private void HandleTutorialSignalCycleCompleted(
        int completedCycleCount
    )
    {
        // STEP4中だけ判定する。
        if (
            currentState !=
            TutorialState.PlayingSignal
        )
        {
            return;
        }


        if (
            completedCycleCount <
            minimumSignalCyclesBeforeLowering
        )
        {
            return;
        }


        // ここでは信号を止めない。
        //
        // STEP5へ進んだ後も、
        // プレイヤーが潜望鏡を完全に格納するまでは
        // 白→赤→青→赤→赤... を繰り返し続ける。
        BeginStep5LowerPeriscope();


        DebugMessage(
            "信号を必要回数確認しました。STEP5へ進みます。"
        );
    }


    private void HandleTutorialSignalFinished()
    {
        // Loop Until StoppedがOFFの場合の保険。
        //
        // 有限再生がSTEP4中に終了した場合も、
        // STEP5へ進める。
        if (
            currentState !=
            TutorialState.PlayingSignal
        )
        {
            return;
        }


        BeginStep5LowerPeriscope();
    }


    private void UnsubscribeSignalEmitter()
    {
        if (tutorialSignalEmitter == null)
        {
            return;
        }


        tutorialSignalEmitter
            .SequenceCycleCompleted -=
                HandleTutorialSignalCycleCompleted;


        tutorialSignalEmitter
            .SignalFinished -=
                HandleTutorialSignalFinished;
    }


    // ============================================================
    // Step 5 - Lower
    // ============================================================

    private void BeginStep5LowerPeriscope()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyButton3();


        currentState =
            TutorialState.WaitForPeriscopeLowered;


        SetTutorialText(
            "STEP 5 / 6",
            lowerInstruction
        );


        videoController
            .PlayLowerPeriscopeVideo();
    }


    private void CheckPeriscopeLowered()
    {
        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            return;
        }


        // 潜望鏡を完全に格納した時点で、
        // STEP4から繰り返していた敵艦信号を停止する。
        if (tutorialSignalEmitter != null)
        {
            tutorialSignalEmitter
                .StopSignal();
        }


        UnsubscribeSignalEmitter();


        // 信号確認が完了したことをMissionManagerへ通知する。
        //
        // 潜望鏡はすでに完全格納されているため、
        // MissionManagerはInputtingへ進める。
        colorMemoryMissionManager
            .NotifyEnemySequenceFinished(
                tutorialEnemyMemoryComponent
            );


        // ここでは入力を無効化しない。
        //
        // STEP5ではButton3（青）だけを許可しているため、
        // 潜望鏡を下げ終えた瞬間も、実際に青ボタンを
        // 押し続けている間はDataManager上のButton3を1のまま保つ。
        //
        // その後CheckInputReady()でButton2～4がすべて離されたことを
        // 確認してからSTEP6へ進む。
        currentState =
            TutorialState.WaitForInputReady;


        videoController
            .HideVideo();
    }


    private void CheckInputReady()
    {
        if (
            colorMemoryMissionManager
                .GetCurrentState()
            !=
            ColorMemoryMissionManager
                .MissionState
                .Inputting
        )
        {
            return;
        }


        // 潜望鏡を下げるために使った青ボタンが
        // まだ押されたままならSTEP6を開始しない。
        //
        // Button2～4がすべて離されたことを確認してから
        // 色入力ボタンを有効化することで、
        // 下げ操作のButton3が最初の「青」として
        // 誤入力されることを防ぐ。
        bool allColorButtonsReleased =
            DataManager
                .GetSensorButton2()
            ==
            0
            &&
            DataManager
                .GetSensorButton3()
            ==
            0
            &&
            DataManager
                .GetSensorButton4()
            ==
            0;


        if (!allColorButtonsReleased)
        {
            return;
        }


        BeginStep6Input(
            false
        );
    }


    // ============================================================
    // Step 6 - Input
    // ============================================================

    private void BeginStep6Input(
        bool isRetry
    )
    {
        DisableAllInputs();


        // Button2=赤、Button3=青、Button4=黄、Button6=白。
        // STEP6では白ボタンも有効にし、
        // 入力途中の色列をリセットできるようにする。
        sensorBridge
            .SetButtonsAllowed(
                false,
                true,
                true,
                true,
                true
            );


        currentState =
            TutorialState.WaitForMissionResult;


        SetTutorialText(
            "STEP 6 / 6",
            isRetry
                ? retryInputInstruction
                : inputInstruction
        );


        videoController
            .PlayInputSignalVideo();
    }


    // ============================================================
    // Result
    // ============================================================

    private void HandleMissionEvaluated(
        bool wasSuccessful
    )
    {
        if (
            currentState !=
            TutorialState.WaitForMissionResult
        )
        {
            return;
        }


        DisableAllInputs();


        if (wasSuccessful)
        {
            StartFlowCoroutine(
                SuccessRoutine()
            );
        }
        else
        {
            StartFlowCoroutine(
                RetryRoutine()
            );
        }
    }


    private IEnumerator SuccessRoutine()
    {
        if (resultDisplayWait > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    resultDisplayWait
                );
        }


        currentState =
            TutorialState.ShowingResetHint;


        SetTutorialText(
            "ヒント",
            resetHintInstruction
        );


        videoController
            .PlayResetSignalVideo();


        if (resetHintDuration > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    resetHintDuration
                );
        }


        flowCoroutine =
            null;


        BeginRiskDemo();
    }


    private IEnumerator RetryRoutine()
    {
        if (retryDelay > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    retryDelay
                );
        }


        // MissionManagerがFailedからSearchingへ戻るのを待つ。
        while (
            colorMemoryMissionManager
                .GetCurrentState()
            !=
            ColorMemoryMissionManager
                .MissionState
                .Searching
        )
        {
            yield return null;
        }


        bool missionStarted =
            colorMemoryMissionManager
                .TryBeginMission(
                    tutorialEnemyMemoryComponent,
                    tutorialSignalEmitter
                        .GetFixedSequence()
                );


        if (!missionStarted)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "再入力用ミッションを開始できませんでした。"
            );


            flowCoroutine =
                null;

            yield break;
        }


        // 潜望鏡はすでに完全格納済みなので、
        // 信号を再表示せず、そのまま入力可能状態へ移す。
        colorMemoryMissionManager
            .NotifyEnemySequenceFinished(
                tutorialEnemyMemoryComponent
            );


        while (
            colorMemoryMissionManager
                .GetCurrentState()
            !=
            ColorMemoryMissionManager
                .MissionState
                .Inputting
        )
        {
            yield return null;
        }


        flowCoroutine =
            null;


        BeginStep6Input(
            true
        );
    }


    // ============================================================
    // Complete
    // ============================================================

    private void BeginTutorialComplete()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyButton1();


        videoController
            .HideVideo();


        currentState =
            TutorialState.Completed;


        completionButtonReleased =
            DataManager
                .GetSensorButton1()
            ==
            0;


        SetTutorialText(
            "COMPLETE",
            completeInstruction
        );
    }


    private void CheckCompleteInput()
    {
        int button1 =
            DataManager
                .GetSensorButton1();


        if (!completionButtonReleased)
        {
            if (button1 == 0)
            {
                completionButtonReleased =
                    true;
            }


            return;
        }


        bool buttonPressed =
            button1 == 1;


        bool enterPressed =
            allowEnterKeyToStart &&
            Keyboard.current != null &&
            (
                Keyboard.current
                    .enterKey
                    .wasPressedThisFrame
                ||
                Keyboard.current
                    .numpadEnterKey
                    .wasPressedThisFrame
            );


        if (
            !buttonPressed &&
            !enterPressed
        )
        {
            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                mainColorSceneName
            )
        )
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "Main Color Scene Nameが空です。"
            );

            return;
        }


        ChangeToMainColorScene();
    }


    // ============================================================
    // Enemy
    // ============================================================

    private bool ResolveTutorialEnemy()
    {
        if (tutorialEnemy != null)
        {
            return true;
        }


        tutorialEnemy =
            enemySpawner
                .GetSpawnedEnemy();


        if (tutorialEnemy == null)
        {
            tutorialEnemy =
                enemySpawner
                    .SpawnTutorialEnemy();
        }


        return
            tutorialEnemy != null;
    }


    // ============================================================
    // Utility
    // ============================================================

    private void SetSonarPanel(
        bool visible
    )
    {
        if (sonarPanel == null)
        {
            return;
        }


        sonarPanel.SetActive(
            visible
        );
    }


    private void DisableAllInputs()
    {
        if (sensorBridge == null)
        {
            return;
        }


        sensorBridge
            .SetAllTutorialInputsAllowed(
                false
            );
    }


    private void SetTutorialText(
        string step,
        string instruction
    )
    {
        if (stepText != null)
        {
            stepText.text =
                step;
        }


        if (instructionText != null)
        {
            instructionText.text =
                instruction;
        }
    }


    private void StartFlowCoroutine(
        IEnumerator routine
    )
    {
        if (flowCoroutine != null)
        {
            StopCoroutine(
                flowCoroutine
            );
        }


        flowCoroutine =
            StartCoroutine(
                routine
            );
    }


    // ============================================================
    // Debug
    // ============================================================

    private void UpdateDebugValues()
    {
        debugButton1 =
            DataManager
                .GetSensorButton1();


        if (sensorRead != null)
        {
            debugRawButton5 =
                ReadRawButton5();
        }


        debugSonarPanelActive =
            sonarPanel != null &&
            sonarPanel.activeSelf;


        debugCurrentStep =
            GetCurrentStepForDebug();
    }


    private void DebugMessage(
        string message
    )
    {
        if (!debugLog)
        {
            return;
        }


        Debug.Log(
            "ColorTutorialFlowController: " +
            message
        );
    }


    public int GetCurrentStepForDebug()
    {
        switch (currentState)
        {
            case TutorialState.WaitForSonarOpen:
            case TutorialState.WaitForSonarRelease:

                return 1;


            case TutorialState.WaitForPeriscopeRaised:

                return 2;


            case TutorialState.WaitForEnemyInView:

                return 3;


            case TutorialState.PlayingSignal:

                return 4;


            case TutorialState.WaitForPeriscopeLowered:
            case TutorialState.WaitForInputReady:

                return 5;


            case TutorialState.WaitForMissionResult:

                return 6;


            case TutorialState.WaitForDangerPeriscopeExposure:
            case TutorialState.WaitForDangerDetection:
            case TutorialState.ShowingDetectionResult:

                return 7;


            default:

                return 0;
        }
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        enemyViewportMargin =
            Mathf.Clamp(
                enemyViewportMargin,
                0.0f,
                0.45f
            );


        requiredEnemyVisibleDuration =
            Mathf.Max(
                0.0f,
                requiredEnemyVisibleDuration
            );


        signalStartDelay =
            Mathf.Max(
                0.0f,
                signalStartDelay
            );


        minimumSignalCyclesBeforeLowering =
            Mathf.Max(
                1,
                minimumSignalCyclesBeforeLowering
            );


        retryDelay =
            Mathf.Max(
                0.0f,
                retryDelay
            );


        resultDisplayWait =
            Mathf.Max(
                0.0f,
                resultDisplayWait
            );


        resetHintDuration =
            Mathf.Max(
                0.0f,
                resetHintDuration
            );


        riskDemoInitialRisk =
            Mathf.Max(
                0.0f,
                riskDemoInitialRisk
            );


        detectionResultDisplayDuration =
            Mathf.Max(
                0.0f,
                detectionResultDisplayDuration
            );
    }
}
