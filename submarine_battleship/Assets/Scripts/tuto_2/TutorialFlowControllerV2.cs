using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class TutorialFlowControllerV2 : MonoBehaviour
{
    // ============================================================
    // Tutorial State
    // ============================================================

    private enum TutorialState
    {
        None = 0,

        Dialogue = 1,

        WaitForRotation = 2,

        WaitForFirstLowering = 3,

        WaitForSonarOpen = 4,

        WaitForSonarClose = 5,

        WaitForRaising = 6,

        WaitForEnemyFound = 7,

        WaitForSignalFinished = 8,

        WaitForSecondLowering = 9,

        WaitForInputReady = 10,

        WaitForSignalInput = 11,

        WaitForTransmission = 12,

        WaitForEvaluation = 13,

        WaitForResult = 14,

        WaitForRetryRaising = 15,

        WaitForRiskIncrease = 16,

        WaitForRiskLowering = 17,

        WaitForRiskRecovery = 18,

        WaitForMissionStart = 19,

        Completed = 20
    }


    // ============================================================
    // 定数
    // ============================================================

    private const string DefaultMainSceneName =
        "MainScene";

    private const string DefaultSpeakerName =
        "司令官";

    private const float DefaultCharacterInterval =
        0.035f;

    private const float DefaultRequiredRotation =
        30.0f;

    private const float DefaultResultWaitDuration =
        2.2f;

    private const float DefaultRaisedHeightTolerance =
        0.05f;

    private const float DefaultTutorialRiskTarget =
        30.0f;

    private const float DefaultRiskRecoveryTarget =
        1.0f;


    // ============================================================
    // Conversation UI
    // ============================================================

    [Header("Conversation UI")]

    [SerializeField]
    private GameObject conversationPanel;


    [SerializeField]
    private Image speakerPortraitImage;


    [SerializeField]
    private Sprite commanderPortrait;


    [SerializeField]
    private TMP_Text speakerNameText;


    [SerializeField]
    private TMP_Text dialogueText;


    [SerializeField, Tooltip(
        "次へ進めるときの下向き三角形など")]
    private GameObject nextDialogueIndicator;


    [SerializeField]
    private CanvasGroup
        nextDialogueIndicatorCanvasGroup;


    // ============================================================
    // Mission UI
    // ============================================================

    [Header("Mission UI")]

    [SerializeField]
    private GameObject missionPanel;


    [SerializeField]
    private TMP_Text missionText;


    // ============================================================
    // Tutorial Systems
    // ============================================================

    [Header("Tutorial Systems")]

    [SerializeField]
    private TutorialSensorBridgeV2
        sensorBridge;


    [SerializeField]
    private TutorialSonarControllerV2
        sonarController;


    [SerializeField]
    private TutorialEnemyControllerV2
        tutorialEnemy;


    // ============================================================
    // Game Systems
    // ============================================================

    [Header("Existing Game Systems")]

    [SerializeField]
    private CommunicationMissionManager
        communicationMissionManager;


    [SerializeField, Tooltip(
        "上下移動するPeriscopeRoot")]
    private Transform periscopeTransform;


    [SerializeField, Tooltip(
        "危険度を管理するExposureRiskManager")]
    private ExposureRiskManager
        exposureRiskManager;


    [SerializeField, Tooltip(
        "Canvas内のExposureRiskUI。" +
        "危険度チュートリアル開始時に表示する")]
    private GameObject
        exposureRiskUI;


    // ============================================================
    // Tutorial Configuration
    // ============================================================

    [Header("Tutorial Configuration")]

    [SerializeField]
    [Min(1.0f)]
    private float requiredRotationDegrees =
        DefaultRequiredRotation;


    [SerializeField]
    [Min(0.001f)]
    private float characterInterval =
        DefaultCharacterInterval;


    [SerializeField]
    [Min(0.0f)]
    private float resultWaitDuration =
        DefaultResultWaitDuration;


    [SerializeField, Tooltip(
        "初期の潜望鏡高さを完全上昇位置として扱う")]
    private bool useInitialHeightAsRaisedPosition =
        true;


    [SerializeField]
    [Min(0.0f)]
    private float raisedHeightTolerance =
        DefaultRaisedHeightTolerance;


    [SerializeField, Tooltip(
        "危険度チュートリアルで、" +
        "ここまで危険度が上がったら次へ進む")]
    [Min(0.0f)]
    private float tutorialRiskTarget =
        DefaultTutorialRiskTarget;


    [SerializeField, Tooltip(
        "潜望鏡格納後、危険度がここまで下がったら" +
        "回復確認完了とする")]
    [Min(0.0f)]
    private float riskRecoveryTarget =
        DefaultRiskRecoveryTarget;


    // ============================================================
    // Scene
    // ============================================================

    [Header("Scene")]

    [SerializeField]
    private string mainSceneName =
        DefaultMainSceneName;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        true;


    // ============================================================
    // Dialogues
    // ============================================================

    [Header("Dialogue - Introduction")]

    [SerializeField, TextArea(2, 5)]
    private string[] introductionDialogue =
    {
        "こちら作戦司令部。通信は聞こえているな。",
        "これから潜望鏡を使った偵察と通信の訓練を行う。",
        "画面に表示される指示に従って操作してくれ。"
    };


    [Header("Dialogue - After Rotation")]

    [SerializeField, TextArea(2, 5)]
    private string[] afterRotationDialogue =
    {
        "よし。潜望鏡の旋回操作は問題ない。",
        "次は潜望鏡を完全に格納してみろ。"
    };


    [Header("Dialogue - Before Sonar")]

    [SerializeField, TextArea(2, 5)]
    private string[] beforeSonarDialogue =
    {
        "潜望鏡を格納した。",
        "水中ではソナーを使って、周囲の船のおおよその位置を確認できる。",
        "黒いボタンを押してソナーを起動してみろ。"
    };


    [Header("Dialogue - After Sonar")]

    [SerializeField, TextArea(2, 5)]
    private string[] afterSonarDialogue =
    {
        "ソナーでは船のおおよその位置は分かる。",
        "ただし、ソナーだけではその船が何者なのかは判断できない。",
        "直接確認するため、潜望鏡を海上まで上げろ。"
    };


    [Header("Dialogue - Before Enemy Search")]

    [SerializeField, TextArea(2, 5)]
    private string[] beforeEnemySearchDialogue =
    {
        "ここからは実際に船を確認する。",
        "先ほどソナーで確認した方向を参考に、潜望鏡を回して敵船を探せ。"
    };


    [Header("Dialogue - Signal Explanation")]

    [SerializeField, TextArea(2, 5)]
    private string[] signalExplanationDialogue =
    {
        "敵船を確認した。",
        "敵船は光を使って通信信号を送ってくる。",
        "黄色の光は、信号の周期が始まることを示す開始合図だ。黄色自体は入力しない。",
        "赤色は短信号「・」、オレンジ色は長信号「―」を表す。",
        "これから同じ信号を2回送る。光の順番を記憶しろ。"
    };


    [Header("Dialogue - After Signal")]

    [SerializeField, TextArea(2, 5)]
    private string[] afterSignalDialogue =
    {
        "信号の送信が終了した。",
        "記憶した信号を司令部へ送る。",
        "まず潜望鏡を完全に格納しろ。"
    };


    [Header("Dialogue - Failure")]

    [SerializeField, TextArea(2, 5)]
    private string[] failureDialogue =
    {
        "信号が一致しなかった。",
        "もう一度敵船の信号を確認する。",
        "潜望鏡を上げて再確認しろ。"
    };


    [Header("Dialogue - Success")]

    [SerializeField, TextArea(2, 5)]
    private string[] successDialogue =
    {
        "通信成功。これが偵察通信の一連の流れだ。",
        "最後に、実戦で重要になる危険度について説明する。"
    };


    [Header("Dialogue - Risk Increased")]

    [SerializeField, TextArea(2, 5)]
    private string[] riskIncreasedDialogue =
    {
        "右上に表示されているゲージが危険度だ。",
        "潜望鏡や通信マストを海上に出している間、危険度は上昇する。",
        "危険度が限界に達すると敵に発見され、スコアを失う。",
        "次は潜望鏡を格納して、危険度が下がることを確認しろ。"
    };


    [Header("Dialogue - Risk Recovered")]

    [SerializeField, TextArea(2, 5)]
    private string[] riskRecoveredDialogue =
    {
        "何も露出していない間は、危険度が徐々に低下する。",
        "ソナーで位置を絞り、必要な時間だけ潜望鏡を使うことが重要だ。",
        "訓練は以上だ。これより実戦任務を開始する。"
    };


    // ============================================================
    // Internal
    // ============================================================

    private TutorialState currentState =
        TutorialState.None;


    private string[] currentDialogueLines;

    private int currentDialogueIndex;

    private Action onDialogueFinished;


    private Coroutine typingCoroutine;

    private Coroutine resultCoroutine;


    private bool isTyping =
        false;


    private int totalCharacterCount =
        0;


    private int previousButton1 =
        0;


    private float previousPeriscopeYaw =
        0.0f;


    private float accumulatedRotation =
        0.0f;


    private float initialRaisedLocalY =
        0.0f;


    private bool isChangingScene =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        Time.timeScale =
            1.0f;


        // Tutorial用ソナーと競合しないように、
        // MainScene用MenuPanelManagerはTutorialScene内だけ停止する。
        MenuPanelManager mainMenuManager =
            FindFirstObjectByType<
                MenuPanelManager
            >();


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


        // ========================================================
        // 危険度チュートリアル開始までは危険度を停止
        // ========================================================

        if (exposureRiskManager != null)
        {
            exposureRiskManager.ResetRisk();

            exposureRiskManager.SetRiskSystemEnabled(
                false
            );
        }


        if (exposureRiskUI != null)
        {
            exposureRiskUI.SetActive(
                false
            );
        }


        if (periscopeTransform != null)
        {
            initialRaisedLocalY =
                periscopeTransform
                    .localPosition
                    .y;
        }


        SetupUI();

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


        PlayDialogueBlock(
            introductionDialogue,
            BeginRotationStep
        );
    }


    // ============================================================
    // OnDestroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeEvents();


        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }


        if (resultCoroutine != null)
        {
            StopCoroutine(
                resultCoroutine
            );
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        UpdateNextIndicatorBlink();


        if (isChangingScene)
        {
            return;
        }


        switch (currentState)
        {
            case TutorialState.Dialogue:

                if (ReadAdvanceInput())
                {
                    HandleDialogueAdvance();
                }

                break;


            case TutorialState.WaitForRotation:

                CheckRotation();

                break;


            case TutorialState.WaitForFirstLowering:

                CheckFirstLowering();

                break;


            case TutorialState.WaitForSonarOpen:

                CheckSonarOpen();

                break;


            case TutorialState.WaitForSonarClose:

                CheckSonarClose();

                break;


            case TutorialState.WaitForRaising:

                CheckRaising(
                    false
                );

                break;


            case TutorialState.WaitForSecondLowering:

                CheckSecondLowering();

                break;


            case TutorialState.WaitForRetryRaising:

                CheckRaising(
                    true
                );

                break;


            case TutorialState.WaitForRiskIncrease:

                CheckRiskIncrease();

                break;


            case TutorialState.WaitForRiskLowering:

                CheckRiskLowering();

                break;


            case TutorialState.WaitForRiskRecovery:

                CheckRiskRecovery();

                break;


            case TutorialState.WaitForMissionStart:

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


        if (
            exposureRiskManager ==
            null
        )
        {
            exposureRiskManager =
                FindFirstObjectByType<
                    ExposureRiskManager
                >();
        }


        if (periscopeTransform == null)
        {
            PeriscopeController
                periscopeController =
                    FindFirstObjectByType<
                        PeriscopeController
                    >();


            if (periscopeController != null)
            {
                periscopeTransform =
                    periscopeController
                        .transform;
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


        if (
            communicationMissionManager !=
            null
        )
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


        if (
            communicationMissionManager !=
            null
        )
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
    // UI初期化
    // ============================================================

    private void SetupUI()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(
                false
            );
        }


        if (missionPanel != null)
        {
            missionPanel.SetActive(
                false
            );
        }


        if (speakerNameText != null)
        {
            speakerNameText.text =
                DefaultSpeakerName;
        }


        if (
            speakerPortraitImage != null &&
            commanderPortrait != null
        )
        {
            speakerPortraitImage.sprite =
                commanderPortrait;
        }


        SetNextIndicatorVisible(
            false
        );
    }


    // ============================================================
    // Dialogue
    // ============================================================

    private void PlayDialogueBlock(
        string[] lines,
        Action finishedAction
    )
    {
        // ========================================================
        // 会話中はConversationPanelだけを表示
        // ========================================================

        HideMission();

        ShowConversationPanel();


        SetAllButtonsDisabled();


        // Dialogue送り用Button1だけ許可
        if (sensorBridge != null)
        {
            sensorBridge.SetButton1Allowed(
                true
            );
        }


        if (sonarController != null)
        {
            sonarController.SetInputEnabled(
                false
            );
        }


        currentDialogueLines =
            lines;


        currentDialogueIndex =
            0;


        onDialogueFinished =
            finishedAction;


        currentState =
            TutorialState.Dialogue;


        previousButton1 =
            DataManager
                .GetSensorButton1();


        if (
            currentDialogueLines == null ||
            currentDialogueLines.Length == 0
        )
        {
            FinishDialogueBlock();

            return;
        }


        ShowDialogueLine(
            currentDialogueLines[
                currentDialogueIndex
            ]
        );
    }


    private void ShowDialogueLine(
        string line
    )
    {
        SetNextIndicatorVisible(
            false
        );


        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }


        typingCoroutine =
            StartCoroutine(
                TypeDialogue(
                    line
                )
            );
    }


    private IEnumerator TypeDialogue(
        string line
    )
    {
        if (dialogueText == null)
        {
            yield break;
        }


        isTyping =
            true;


        dialogueText.text =
            line;


        dialogueText.maxVisibleCharacters =
            0;


        dialogueText.ForceMeshUpdate();


        totalCharacterCount =
            dialogueText
                .textInfo
                .characterCount;


        for (
            int visibleCharacters = 0;
            visibleCharacters <=
            totalCharacterCount;
            visibleCharacters++
        )
        {
            dialogueText.maxVisibleCharacters =
                visibleCharacters;


            if (
                visibleCharacters <
                totalCharacterCount
            )
            {
                yield return
                    new WaitForSecondsRealtime(
                        characterInterval
                    );
            }
        }


        isTyping =
            false;


        typingCoroutine =
            null;


        SetNextIndicatorVisible(
            true
        );
    }


    private void HandleDialogueAdvance()
    {
        if (isTyping)
        {
            ShowAllCharacters();

            return;
        }


        currentDialogueIndex++;


        if (
            currentDialogueLines != null &&
            currentDialogueIndex <
            currentDialogueLines.Length
        )
        {
            ShowDialogueLine(
                currentDialogueLines[
                    currentDialogueIndex
                ]
            );


            return;
        }


        FinishDialogueBlock();
    }


    private void ShowAllCharacters()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );


            typingCoroutine =
                null;
        }


        if (dialogueText != null)
        {
            dialogueText
                .maxVisibleCharacters =
                    int.MaxValue;
        }


        isTyping =
            false;


        SetNextIndicatorVisible(
            true
        );
    }


    private void FinishDialogueBlock()
    {
        SetNextIndicatorVisible(
            false
        );


        HideConversationPanel();


        Action action =
            onDialogueFinished;


        currentDialogueLines =
            null;


        onDialogueFinished =
            null;


        currentState =
            TutorialState.None;


        action?.Invoke();
    }


    // ============================================================
    // Conversation Panel
    // ============================================================

    private void ShowConversationPanel()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(
                true
            );
        }
    }


    private void HideConversationPanel()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(
                false
            );
        }


        SetNextIndicatorVisible(
            false
        );
    }


    // ============================================================
    // Rotation
    // ============================================================

    private void BeginRotationStep()
    {
        SetAllButtonsDisabled();


        currentState =
            TutorialState.WaitForRotation;


        accumulatedRotation =
            0.0f;


        previousPeriscopeYaw =
            DataManager
                .GetPeriscopeRotation();


        ShowMission(
            1,
            "潜望鏡をゆっくり左右に回してください。"
        );
    }


    private void CheckRotation()
    {
        float currentYaw =
            DataManager
                .GetPeriscopeRotation();


        float delta =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    previousPeriscopeYaw,
                    currentYaw
                )
            );


        if (delta <= 90.0f)
        {
            accumulatedRotation +=
                delta;
        }


        previousPeriscopeYaw =
            currentYaw;


        if (
            accumulatedRotation <
            requiredRotationDegrees
        )
        {
            return;
        }


        PlayDialogueBlock(
            afterRotationDialogue,
            BeginFirstLowering
        );
    }


    // ============================================================
    // First Lowering
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
            TutorialState.WaitForFirstLowering;


        ShowMission(
            2,
            "青いボタンを押し続け、潜望鏡を完全に格納してください。"
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


        PlayDialogueBlock(
            beforeSonarDialogue,
            BeginSonarStep
        );
    }


    // ============================================================
    // Sonar
    // ============================================================

    private void BeginSonarStep()
    {
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
            TutorialState.WaitForSonarOpen;


        ShowMission(
            3,
            "黒いボタンを押し続けてソナーを表示してください。"
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
            TutorialState.WaitForSonarClose;


        ShowMission(
            4,
            "ソナーで船の位置を確認し、黒いボタンを離してください。"
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


        PlayDialogueBlock(
            afterSonarDialogue,
            BeginRaising
        );
    }


    // ============================================================
    // Raising
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
            TutorialState.WaitForRaising;


        ShowMission(
            5,
            "赤いボタンを押し続けて潜望鏡を海上まで上げてください。"
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
            StartCoroutine(
                BeginRetrySignalWhenReady()
            );


            return;
        }


        PlayDialogueBlock(
            beforeEnemySearchDialogue,
            BeginEnemySearch
        );
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


    // ============================================================
    // Enemy Search
    // ============================================================

    private void BeginEnemySearch()
    {
        SetAllButtonsDisabled();


        currentState =
            TutorialState.WaitForEnemyFound;


        ShowMission(
            6,
            "潜望鏡を回して敵船を発見してください。"
        );


        if (tutorialEnemy != null)
        {
            tutorialEnemy
                .SetDetectionEnabled(
                    true
                );
        }
    }


    private void HandleEnemyFound()
    {
        if (
            currentState !=
            TutorialState.WaitForEnemyFound
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        PlayDialogueBlock(
            signalExplanationDialogue,
            BeginSignalObservation
        );
    }


    // ============================================================
    // Signal Observation
    // ============================================================

    private void BeginSignalObservation()
    {
        SetAllButtonsDisabled();


        currentState =
            TutorialState.WaitForSignalFinished;


        ShowMission(
            7,
            "敵船の光信号を観察し、順番を記憶してください。"
        );


        if (
            tutorialEnemy == null ||
            !tutorialEnemy
                .BeginSignalMission()
        )
        {
            Debug.LogError(
                "Tutorial: " +
                "発光信号を開始できませんでした。"
            );
        }
    }


    private void HandleSignalFinished()
    {
        if (
            currentState !=
            TutorialState.WaitForSignalFinished
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        PlayDialogueBlock(
            afterSignalDialogue,
            BeginSecondLowering
        );
    }


    // ============================================================
    // Second Lowering
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
            TutorialState.WaitForSecondLowering;


        ShowMission(
            8,
            "青いボタンを押し続け、潜望鏡を完全に格納してください。"
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
            TutorialState.WaitForInputReady;


        ShowMission(
            9,
            "通信入力の準備を待ってください。"
        );
    }


    // ============================================================
    // Mission State
    // ============================================================

    private void HandleMissionStateChanged(
        CommunicationMissionManager
            .MissionState state
    )
    {
        switch (state)
        {
            case CommunicationMissionManager
                .MissionState
                .Inputting:

                if (
                    currentState ==
                        TutorialState.WaitForSecondLowering
                    ||
                    currentState ==
                        TutorialState.WaitForInputReady
                )
                {
                    BeginSignalInput();
                }

                break;


            case CommunicationMissionManager
                .MissionState
                .Transmitting:

                if (
                    currentState ==
                    TutorialState.WaitForSignalInput
                )
                {
                    BeginTransmissionWait();
                }

                break;


            case CommunicationMissionManager
                .MissionState
                .Evaluating:

                if (
                    currentState ==
                        TutorialState.WaitForTransmission
                    ||
                    currentState ==
                        TutorialState.WaitForSignalInput
                )
                {
                    BeginEvaluationWait();
                }

                break;
        }
    }


    // ============================================================
    // Signal Input
    // ============================================================

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
            TutorialState.WaitForSignalInput;


        ShowMission(
            10,
            "黄色いボタンで信号を入力してください。\n" +
            "短く押す = ・　長く押す = ―"
        );
    }


    // ============================================================
    // Transmission
    // ============================================================

    private void BeginTransmissionWait()
    {
        SetAllButtonsDisabled();


        currentState =
            TutorialState.WaitForTransmission;


        ShowMission(
            0,
            "司令部へ信号を送信しています。\n" +
            "操作せずお待ちください。"
        );
    }


    private void BeginEvaluationWait()
    {
        SetAllButtonsDisabled();


        currentState =
            TutorialState.WaitForEvaluation;


        ShowMission(
            0,
            "送信した信号を照合しています。\n" +
            "操作せずお待ちください。"
        );
    }


    // ============================================================
    // Result
    // ============================================================

    private void HandleMissionEvaluated(
        bool successful
    )
    {
        SetAllButtonsDisabled();


        currentState =
            TutorialState.WaitForResult;


        HideMission();


        if (resultCoroutine != null)
        {
            StopCoroutine(
                resultCoroutine
            );
        }


        resultCoroutine =
            StartCoroutine(
                successful
                    ? SuccessResultRoutine()
                    : FailureResultRoutine()
            );
    }


    private IEnumerator SuccessResultRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                resultWaitDuration
            );


        resultCoroutine =
            null;


        PlayDialogueBlock(
            successDialogue,
            BeginRiskTutorial
        );
    }


    private IEnumerator FailureResultRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                resultWaitDuration
            );


        resultCoroutine =
            null;


        PlayDialogueBlock(
            failureDialogue,
            BeginRetryRaising
        );
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
            TutorialState.WaitForRetryRaising;


        ShowMission(
            11,
            "赤いボタンを押し続け、もう一度潜望鏡を上げてください。"
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


        currentState =
            TutorialState.WaitForSignalFinished;


        ShowMission(
            7,
            "敵船の光信号をもう一度記憶してください。"
        );


        if (
            tutorialEnemy == null ||
            !tutorialEnemy
                .BeginSignalMission()
        )
        {
            Debug.LogError(
                "Tutorial: " +
                "再試行用の信号を開始できませんでした。"
            );
        }
    }


    // ============================================================
    // Risk Tutorial
    // ============================================================

    private void BeginRiskTutorial()
    {
        SetAllButtonsDisabled();


        if (exposureRiskManager == null)
        {
            Debug.LogError(
                "Tutorial: ExposureRiskManagerが見つかりません。"
            );


            // 危険度システムが見つからない場合でも
            // チュートリアル全体が停止しないよう、
            // 最終説明へ進む。
            PlayDialogueBlock(
                riskRecoveredDialogue,
                BeginMissionStart
            );


            return;
        }


        if (exposureRiskUI != null)
        {
            exposureRiskUI.SetActive(
                true
            );
        }


        exposureRiskManager.enabled =
            true;


        exposureRiskManager.ResetRisk();

        exposureRiskManager.SetRiskSystemEnabled(
            true
        );


        if (sensorBridge != null)
        {
            sensorBridge.SetButton2Allowed(
                true
            );
        }


        currentState =
            TutorialState.WaitForRiskIncrease;


        ShowMission(
            12,
            "赤いボタンで潜望鏡を上げ、危険度が上昇することを確認してください。"
        );


        if (debugLog)
        {
            Debug.Log(
                "Tutorial: 危険度上昇確認開始"
            );
        }
    }


    private void CheckRiskIncrease()
    {
        if (exposureRiskManager == null)
        {
            return;
        }


        float currentRisk =
            exposureRiskManager
                .GetCurrentRisk();


        if (
            currentRisk <
            tutorialRiskTarget
        )
        {
            return;
        }


        // ========================================================
        // 説明中に危険度が上がり続けないよう一時停止
        // ========================================================

        // 危険度の内部設定は有効のまま、
        // MonoBehaviourの更新だけ止めて現在値を固定する。
        // これにより危険度UIに
        // 「危険度システム停止」と表示されるのを防ぐ。
        exposureRiskManager.enabled =
            false;


        SetAllButtonsDisabled();


        PlayDialogueBlock(
            riskIncreasedDialogue,
            BeginRiskLowering
        );


        if (debugLog)
        {
            Debug.Log(
                "Tutorial: 危険度が目標値へ到達 = " +
                currentRisk
            );
        }
    }


    private void BeginRiskLowering()
    {
        SetAllButtonsDisabled();


        // 危険度は説明時の値で一旦固定したまま、
        // 先に潜望鏡を完全格納させる。
        if (exposureRiskManager != null)
        {
            exposureRiskManager.enabled =
                false;
        }


        if (sensorBridge != null)
        {
            sensorBridge.SetButton3Allowed(
                true
            );
        }


        currentState =
            TutorialState.WaitForRiskLowering;


        ShowMission(
            13,
            "青いボタンで潜望鏡を完全に格納してください。"
        );
    }


    private void CheckRiskLowering()
    {
        if (
            !DataManager
                .GetIsPeriscopeFullyLowered()
        )
        {
            return;
        }


        SetAllButtonsDisabled();


        if (exposureRiskManager != null)
        {
            exposureRiskManager.enabled =
                true;


            exposureRiskManager
                .SetRiskSystemEnabled(
                    true
                );
        }


        currentState =
            TutorialState.WaitForRiskRecovery;


        ShowMission(
            14,
            "危険度が低下していくことを確認してください。"
        );


        if (debugLog)
        {
            Debug.Log(
                "Tutorial: 危険度回復確認開始"
            );
        }
    }


    private void CheckRiskRecovery()
    {
        if (exposureRiskManager == null)
        {
            return;
        }


        float currentRisk =
            exposureRiskManager
                .GetCurrentRisk();


        if (
            currentRisk >
            riskRecoveryTarget
        )
        {
            return;
        }


        exposureRiskManager.ResetRisk();


        SetAllButtonsDisabled();


        PlayDialogueBlock(
            riskRecoveredDialogue,
            BeginMissionStart
        );


        if (debugLog)
        {
            Debug.Log(
                "Tutorial: 危険度回復確認完了"
            );
        }
    }


    // ============================================================
    // Tutorial Complete
    // ============================================================

    private void BeginMissionStart()
    {
        SetAllButtonsDisabled();


        if (sensorBridge != null)
        {
            sensorBridge.SetButton1Allowed(
                true
            );
        }


        currentState =
            TutorialState.WaitForMissionStart;


        previousButton1 =
            DataManager
                .GetSensorButton1();


        ShowMission(
            15,
            "黒いボタンまたはEnterキーを押して実戦任務を開始してください。"
        );
    }


    // ============================================================
    // Scene Change
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
            TutorialState.Completed;


        Time.timeScale =
            1.0f;


        SceneManager.LoadScene(
            mainSceneName
        );
    }


    // ============================================================
    // Controls
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
    // Dialogue Advance Input
    // ============================================================

    private bool ReadAdvanceInput()
    {
        bool pressed =
            false;


        int currentButton1 =
            DataManager
                .GetSensorButton1();


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
    // Mission UI
    // ============================================================

    private void ShowMission(
        int number,
        string instruction
    )
    {
        // ========================================================
        // ミッション中はConversationPanelを消す
        // ========================================================

        HideConversationPanel();


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


        if (number > 0)
        {
            missionText.text =
                "MISSION " +
                GetCircledNumber(
                    number
                )
                +
                "："
                +
                instruction;
        }
        else
        {
            // 送信中・照合中などはMISSION番号なし
            missionText.text =
                instruction;
        }
    }


    private void HideMission()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(
                false
            );
        }


        if (missionText != null)
        {
            missionText.text =
                string.Empty;
        }
    }


    private string GetCircledNumber(
        int number
    )
    {
        return
            number switch
            {
                1 => "①",
                2 => "②",
                3 => "③",
                4 => "④",
                5 => "⑤",
                6 => "⑥",
                7 => "⑦",
                8 => "⑧",
                9 => "⑨",
                10 => "⑩",
                11 => "⑪",
                12 => "⑫",
                13 => "⑬",
                14 => "⑭",
                15 => "⑮",
                _ => number.ToString()
            };
    }


    // ============================================================
    // Next Indicator
    // ============================================================

    private void SetNextIndicatorVisible(
        bool visible
    )
    {
        if (nextDialogueIndicator != null)
        {
            nextDialogueIndicator.SetActive(
                visible
            );
        }
    }


    private void UpdateNextIndicatorBlink()
    {
        if (
            nextDialogueIndicator == null ||
            !nextDialogueIndicator.activeSelf ||
            nextDialogueIndicatorCanvasGroup ==
            null
        )
        {
            return;
        }


        float value =
            Mathf.Sin(
                Time.unscaledTime *
                Mathf.PI *
                3.0f
            );


        value =
            value *
            0.5f +
            0.5f;


        nextDialogueIndicatorCanvasGroup.alpha =
            Mathf.Lerp(
                0.25f,
                1.0f,
                value
            );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        requiredRotationDegrees =
            Mathf.Max(
                1.0f,
                requiredRotationDegrees
            );


        characterInterval =
            Mathf.Max(
                0.001f,
                characterInterval
            );


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


        tutorialRiskTarget =
            Mathf.Max(
                0.0f,
                tutorialRiskTarget
            );


        riskRecoveryTarget =
            Mathf.Max(
                0.0f,
                riskRecoveryTarget
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