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
        Completed = 10
    }


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField]
    private ColorTutorialSensorBridge sensorBridge;


    [SerializeField]
    private ColorTutorialVideoController videoController;


    [SerializeField]
    private ColorTutorialEnemySpawner enemySpawner;


    [SerializeField]
    private ColorMemoryMissionManager colorMemoryMissionManager;


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
        "覚えた色を\n順番に入力してください。";


    [SerializeField, TextArea(2, 4)]
    private string retryInputInstruction =
        "入力が違いました。\nもう一度、同じ順番で入力してください。";


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


        SubscribeMissionEvents();


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


            case TutorialState.Completed:

                CheckCompleteInput();

                break;


            case TutorialState.PlayingSignal:
            case TutorialState.WaitForMissionResult:
            case TutorialState.ShowingResetHint:
            case TutorialState.None:
            default:

                break;
        }
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


    private void HandleTutorialSignalFinished()
    {
        if (
            currentState !=
            TutorialState.PlayingSignal
        )
        {
            return;
        }


        UnsubscribeSignalEmitter();


        colorMemoryMissionManager
            .NotifyEnemySequenceFinished(
                tutorialEnemyMemoryComponent
            );


        BeginStep5LowerPeriscope();
    }


    private void UnsubscribeSignalEmitter()
    {
        if (tutorialSignalEmitter == null)
        {
            return;
        }


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


        // ここでは入力を無効化しない。
        //
        // STEP5ではButton3（青）だけを許可しているため、
        // 潜望鏡を下げ終えた瞬間も、実際に青ボタンを
        // 押し続けている間はDataManager上のButton3を1のまま保つ。
        //
        // これによりColorSequenceInputController側の
        // 「入力開始時にButton2～4が押されていたら、
        // 一度すべて離すまで入力しない」
        // という保護処理を正しく働かせることができる。
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


        // Button2=赤、Button3=青、Button4=黄。
        // 白Button6は今回は説明のみなので操作練習では無効。
        sensorBridge
            .SetButtonsAllowed(
                false,
                true,
                true,
                true,
                false
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


        BeginTutorialComplete();
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


        SceneManager.LoadScene(
            mainColorSceneName
        );
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
    }
}
