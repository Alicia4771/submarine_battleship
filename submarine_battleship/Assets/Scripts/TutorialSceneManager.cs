using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialSceneManager : MonoBehaviour
{
    private enum TutorialState
    {
        None,
        Dialogue,

        WaitForRotation,
        WaitForLowering,
        WaitForSonarOpen,
        WaitForSonarClose,
        WaitForRaising,
        WaitForEnemyFound,
        WaitForSignalFinished,
        WaitForMissionStart,

        Completed
    }

    // =========================================================
    // Inspector設定
    // =========================================================

    [Header("センサ")]
    [SerializeField, Tooltip("シリアル通信を行うSensorRead")]
    private SensorRead sensorRead;


    [Header("会話UI")]
    [SerializeField, Tooltip("会話パネル全体")]
    private GameObject conversationPanel;

    [SerializeField, Tooltip("司令官の画像を表示するImage")]
    private Image speakerPortraitImage;

    [SerializeField, Tooltip("司令官の画像")]
    private Sprite commanderPortrait;

    [SerializeField, Tooltip("話者名を表示するTextMeshPro")]
    private TMP_Text speakerNameText;

    [SerializeField, Tooltip("セリフを表示するTextMeshPro")]
    private TMP_Text dialogueText;


    [Header("次のセリフ表示")]
    [SerializeField, Tooltip("次へ進めるときに表示する下向き三角形")]
    private GameObject nextDialogueIndicator;

    [SerializeField, Tooltip("三角形の透明度を制御するCanvasGroup")]
    private CanvasGroup nextDialogueIndicatorCanvasGroup;

    [SerializeField, Tooltip("三角形が1秒間に点滅する回数")]
    [Min(0.1f)]
    private float nextIndicatorBlinkSpeed = 1.5f;

    [SerializeField, Tooltip("点滅時の最小透明度")]
    [Range(0f, 1f)]
    private float nextIndicatorMinimumAlpha = 0.2f;


    [Header("ミッションUI")]
    [SerializeField, Tooltip("操作指示を表示するパネル")]
    private GameObject missionPanel;

    [SerializeField, Tooltip("操作指示を表示するTextMeshPro")]
    private TMP_Text missionText;


    [Header("文字送り")]
    [SerializeField, Tooltip("1文字を表示する間隔")]
    [Min(0.001f)]
    private float characterInterval = 0.04f;


    [Header("潜望鏡")]
    [SerializeField, Tooltip("潜望鏡の親オブジェクト")]
    private Transform periscopeTransform;

    [SerializeField, Tooltip("このY座標以下で水中と判定")]
    private float underwaterYThreshold = -0.1f;

    [SerializeField, Tooltip("このY座標以上で海上と判定")]
    private float raisedYThreshold = 0.1f;

    [SerializeField, Tooltip("潜望鏡を沈めるencode値")]
    private int encoderValueForLowering = -1;

    [SerializeField, Tooltip("潜望鏡を上げるencode値")]
    private int encoderValueForRaising = 1;


    [Header("回転操作")]
    [SerializeField, Tooltip("回転操作成功とする合計角度")]
    [Min(1f)]
    private float requiredRotationDegrees = 30f;

    [SerializeField, Tooltip("1回でこれ以上変化したヨー角は異常値として無視")]
    [Range(10f, 180f)]
    private float maxAcceptedYawDelta = 90f;


    [Header("ソナー")]
    [SerializeField, Tooltip("チュートリアル用ソナー管理スクリプト")]
    private TutorialSceneMenuPanelManager menuPanelManager;


    [Header("敵船")]
    [SerializeField, Tooltip("チュートリアル用の敵船")]
    private TutorialEnemyShip tutorialEnemyShip;


    [Header("シーン")]
    [SerializeField]
    private string mainSceneName = "MainScene";


    [Header("デバッグ")]
    [SerializeField, Tooltip("F2キーで現在の操作を成功扱いにする")]
    private bool allowDebugStepComplete = true;


    // =========================================================
    // 内部変数
    // =========================================================

    private TutorialState currentState = TutorialState.None;

    private string[] currentDialogueLines;
    private int currentDialogueIndex;
    private Action onDialogueFinished;

    private Coroutine typingCoroutine;
    private bool isTyping;
    private int totalCharacterCount;

    private int previousTactileSwitch;

    private float lastYaw;
    private float accumulatedRotation;

    private bool waitingForSonarButtonRelease;
    private bool isChangingScene;


    // =========================================================
    // セリフ
    // =========================================================

    private readonly string[] introductionDialogue =
    {
        "こちら作戦司令部。通信は聞こえているな。",

        "私は、今回の偵察任務を指揮する司令官だ。",

        "こちらから君の声を受信することはできない。返答は不要だ。",

        "これから伝える指示に従って、潜望鏡を操作してくれ。",

        "我々の潜水艦は現在、情報収集のため敵海域へ接近している。",

        "潜水艦の操縦は、別の乗組員が担当している。",

        "君の担当は、潜望鏡を使った周囲の索敵と、敵船が発する通信の傍受だ。",

        "敵船は、光を点灯、消灯させることで仲間と通信している。",

        "これは通常のモールス信号とは異なる、敵独自の信号だ。",

        "今回は、敵船を発見し、通信を傍受するところまでを訓練する。"
    };


    private readonly string[] rotationInstructionDialogue =
    {
        "まずは、潜望鏡の基本操作を確認する。",

        "目の前にある装置が、今回使用する試験用の潜望鏡だ。",

        "潜望鏡を左右に回すと、その動きに合わせて見ている方向も変化する。",

        "急激に回す必要はない。周囲を確認するように、ゆっくり操作するんだ。",

        "潜望鏡をゆっくり左右に回してみてくれ。"
    };


    private readonly string[] loweringInstructionDialogue =
    {
        "よし。潜望鏡の回転を確認した。",

        "潜望鏡は全方向へ回すことができる。",

        "敵船を捜索するときは、一方向だけでなく、周囲を広く確認するんだ。",

        "次は、潜望鏡の高さを変更する。",

        "つまみを右に回し、潜望鏡を水中へ沈めてくれ。"
    };


    private readonly string[] sonarOpenInstructionDialogue =
    {
        "潜望鏡の格納を確認した。",

        "潜望鏡が水中にある間は、海上を直接見ることはできない。",

        "その代わり、周囲の状況をソナーで確認できる。",

        "ソナーには、周囲にいる船のおおよその位置が表示される。",

        "スイッチを押している間、ソナー画面が表示される。",

        "ボタンを押し続けて、周囲を確認してくれ。"
    };


    private readonly string[] sonarFinishedDialogue =
    {
        "ソナーの起動を確認した。",

        "ソナーを使えば、周囲にいる船の位置をある程度把握できる。",

        "ただし、ソナーだけでは、その船が敵か味方かを正確に識別することはできない。",

        "敵船の姿や通信を確認するには、潜望鏡を海上へ出して直接観察する必要がある。",

        "続いて、潜望鏡を海上へ戻す。",

        "つまみを左に回し、潜望鏡を上昇させてくれ。"
    };


    private readonly string[] enemySearchInstructionDialogue =
    {
        "潜望鏡が海上へ出た。",

        "これで、周囲の船を直接確認できる。",

        "ここからは、実際の索敵を行う。",

        "潜望鏡をゆっくり回し、周囲にいる敵船を探してくれ。",

        "船体の形をよく見て、敵船を視界に捉えるんだ。"
    };


    private readonly string[] enemyFoundDialogue =
    {
        "敵船を確認した。",

        "そのまま敵船を視界に捉えておけ。",

        "敵船が通信を開始する。",

        "光の点灯と消灯の順番をよく観察しろ。"
    };


    private readonly string[] tutorialCompleteDialogue =
    {
        "通信の傍受を確認した。",

        "今見た光の順番が、敵の通信信号だ。",

        "本来は、この信号を記憶し、後方にいる味方の船を探して送り返す。",

        "現在、味方への信号送信システムは調整中だ。",

        "今回の訓練では、敵船を発見し、通信を確認できれば任務完了とする。",

        "これで、現在使用できるシステムの説明は終了だ。",

        "潜水艦の航行は、他の乗組員が担当する。",

        "君は潜望鏡を操作し、周囲の索敵に集中してくれ。",

        "ソナーで位置を探し、潜望鏡で直接確認する。この二つを使い分けるんだ。",

        "訓練は以上だ。",

        "これより、敵海域での偵察任務を開始する。",

        "敵船を発見し、送られてくる光の信号を見逃すな。",

        "健闘を祈る。"
    };


    // =========================================================
    // Unityイベント
    // =========================================================

    private void Awake()
    {
        // 前回の敵船一覧などを初期化する
        DataManager.Initialize();
    }


    private void Start()
    {
        FindReferences();
        SetupConversationUI();

        if (menuPanelManager != null)
        {
            menuPanelManager.SetSonarInputEnabled(false);
            menuPanelManager.CloseSonarPanel();
        }

        if (tutorialEnemyShip != null)
        {
            tutorialEnemyShip.SetDetectionEnabled(false);
        }

        if (sensorRead != null)
        {
            previousTactileSwitch =
                sensorRead.GetTactileSwitch();

            DataManager.SetSensorYaw(
                sensorRead.GetYaw()
            );
        }

        UpdatePeriscopeData();

        PlayDialogueBlock(
            introductionDialogue,
            StartRotationExplanation
        );
    }


    private void Update()
    {
        UpdateSensorData();
        UpdatePeriscopeData();

        // 下向き三角形を点滅させる
        UpdateNextDialogueIndicatorBlink();

        if (isChangingScene)
        {
            return;
        }

        if (allowDebugStepComplete &&
            Keyboard.current != null &&
            Keyboard.current.f2Key.wasPressedThisFrame)
        {
            DebugCompleteCurrentStep();
            return;
        }

        bool advancePressed =
            ReadAdvanceInput();

        switch (currentState)
        {
            case TutorialState.Dialogue:

                if (advancePressed)
                {
                    HandleDialogueAdvance();
                }

                break;


            case TutorialState.WaitForRotation:

                CheckPeriscopeRotation();
                break;


            case TutorialState.WaitForLowering:

                CheckPeriscopeLowering();
                break;


            case TutorialState.WaitForSonarOpen:

                CheckSonarOpened();
                break;


            case TutorialState.WaitForSonarClose:

                CheckSonarClosed();
                break;


            case TutorialState.WaitForRaising:

                CheckPeriscopeRaising();
                break;


            case TutorialState.WaitForMissionStart:

                if (advancePressed)
                {
                    ChangeToMainScene();
                }

                break;
        }
    }


    private void OnDisable()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        SetNextDialogueIndicatorVisible(false);
        HideMission();

        if (menuPanelManager != null)
        {
            menuPanelManager.SetSonarInputEnabled(false);
            menuPanelManager.CloseSonarPanel();
        }

        if (tutorialEnemyShip != null)
        {
            tutorialEnemyShip.SetDetectionEnabled(false);
        }
    }


    // =========================================================
    // 初期設定
    // =========================================================

    private void FindReferences()
    {
        if (sensorRead == null)
        {
            sensorRead =
                FindFirstObjectByType<SensorRead>();
        }

        if (periscopeTransform == null)
        {
            Submarine submarine =
                FindFirstObjectByType<Submarine>();

            if (submarine != null)
            {
                periscopeTransform =
                    submarine.transform;
            }
        }

        if (menuPanelManager == null)
        {
            menuPanelManager =
                FindFirstObjectByType<
                    TutorialSceneMenuPanelManager
                >();
        }

        if (tutorialEnemyShip == null)
        {
            tutorialEnemyShip =
                FindFirstObjectByType<
                    TutorialEnemyShip
                >();
        }
    }


    private void SetupConversationUI()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(true);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text =
                "司令官";
        }

        if (speakerPortraitImage != null)
        {
            if (commanderPortrait != null)
            {
                speakerPortraitImage.sprite =
                    commanderPortrait;
            }

            speakerPortraitImage.preserveAspect =
                true;

            speakerPortraitImage.enabled =
                speakerPortraitImage.sprite != null;
        }

        if (nextDialogueIndicator != null &&
            nextDialogueIndicatorCanvasGroup == null)
        {
            nextDialogueIndicatorCanvasGroup =
                nextDialogueIndicator.GetComponent<CanvasGroup>();
        }

        SetNextDialogueIndicatorVisible(false);
        HideMission();
    }


    private void UpdateSensorData()
    {
        if (sensorRead == null)
        {
            return;
        }

        DataManager.SetSensorYaw(
            sensorRead.GetYaw()
        );
    }


    private void UpdatePeriscopeData()
    {
        if (periscopeTransform == null)
        {
            return;
        }

        DataManager.SetSubmarinePosition(
            periscopeTransform.position
        );

        DataManager.SetSubmarineRotation(
            periscopeTransform.eulerAngles.y
        );
    }


    // =========================================================
    // 次のセリフ表示
    // =========================================================

    private void SetNextDialogueIndicatorVisible(
        bool visible)
    {
        if (nextDialogueIndicator != null)
        {
            nextDialogueIndicator.SetActive(
                visible
            );
        }

        if (nextDialogueIndicatorCanvasGroup != null)
        {
            nextDialogueIndicatorCanvasGroup.alpha =
                1f;
        }
    }


    private void UpdateNextDialogueIndicatorBlink()
    {
        if (nextDialogueIndicator == null ||
            !nextDialogueIndicator.activeSelf ||
            nextDialogueIndicatorCanvasGroup == null)
        {
            return;
        }

        float blinkValue =
            (
                Mathf.Sin(
                    Time.unscaledTime *
                    nextIndicatorBlinkSpeed *
                    Mathf.PI *
                    2f
                ) + 1f
            ) * 0.5f;

        nextDialogueIndicatorCanvasGroup.alpha =
            Mathf.Lerp(
                nextIndicatorMinimumAlpha,
                1f,
                blinkValue
            );
    }


    // =========================================================
    // ミッション表示
    // =========================================================

    private void ShowMission(
        int missionNumber,
        string instruction)
    {
        SetNextDialogueIndicatorVisible(false);

        if (missionPanel != null)
        {
            missionPanel.SetActive(true);
        }

        if (missionText != null)
        {
            missionText.text =
                $"MISSION {GetCircledMissionNumber(missionNumber)}：{instruction}";
        }
    }


    private void HideMission()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        if (missionText != null)
        {
            missionText.text =
                string.Empty;
        }
    }


    private string GetCircledMissionNumber(
        int missionNumber)
    {
        return missionNumber switch
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
            _ => missionNumber.ToString()
        };
    }


    // =========================================================
    // 入力
    // =========================================================

    private bool ReadAdvanceInput()
    {
        bool pressed = false;

        if (sensorRead != null)
        {
            int currentTactileSwitch =
                sensorRead.GetTactileSwitch();

            if (currentTactileSwitch == 1 &&
                previousTactileSwitch != 1)
            {
                pressed = true;
            }

            previousTactileSwitch =
                currentTactileSwitch;
        }

        if (Keyboard.current != null &&
            Keyboard.current.enterKey
                .wasPressedThisFrame)
        {
            pressed = true;
        }

        Gamepad gamepad =
            Gamepad.current;

        if (gamepad != null &&
            gamepad.buttonEast
                .wasPressedThisFrame)
        {
            pressed = true;
        }

        return pressed;
    }


    // =========================================================
    // 会話処理
    // =========================================================

    private void PlayDialogueBlock(
        string[] lines,
        Action finishedAction)
    {
        HideMission();
        SetNextDialogueIndicatorVisible(false);

        if (dialogueText == null)
        {
            Debug.LogError(
                "Dialogue Textが設定されていません。"
            );

            return;
        }

        if (lines == null ||
            lines.Length == 0)
        {
            finishedAction?.Invoke();
            return;
        }

        currentDialogueLines =
            lines;

        currentDialogueIndex =
            0;

        onDialogueFinished =
            finishedAction;

        currentState =
            TutorialState.Dialogue;

        ShowDialogueLine(
            currentDialogueLines[
                currentDialogueIndex
            ]
        );
    }


    private void HandleDialogueAdvance()
    {
        if (isTyping)
        {
            ShowAllCharacters();
            return;
        }

        SetNextDialogueIndicatorVisible(false);

        currentDialogueIndex++;

        if (currentDialogueIndex <
            currentDialogueLines.Length)
        {
            ShowDialogueLine(
                currentDialogueLines[
                    currentDialogueIndex
                ]
            );

            return;
        }

        Action finishedAction =
            onDialogueFinished;

        currentDialogueLines =
            null;

        onDialogueFinished =
            null;

        currentState =
            TutorialState.None;

        finishedAction?.Invoke();
    }


    private void ShowDialogueLine(
        string line)
    {
        SetNextDialogueIndicatorVisible(false);

        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }

        typingCoroutine =
            StartCoroutine(
                TypeDialogue(line)
            );
    }


    private IEnumerator TypeDialogue(
        string line)
    {
        isTyping = true;

        SetNextDialogueIndicatorVisible(false);

        dialogueText.text =
            line;

        dialogueText.maxVisibleCharacters =
            0;

        dialogueText.ForceMeshUpdate();

        totalCharacterCount =
            dialogueText.textInfo
                .characterCount;

        for (int visibleCharacters = 0;
             visibleCharacters <= totalCharacterCount;
             visibleCharacters++)
        {
            dialogueText.maxVisibleCharacters =
                visibleCharacters;

            if (visibleCharacters <
                totalCharacterCount)
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

        // 全文表示後に三角形を表示
        SetNextDialogueIndicatorVisible(true);
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

        dialogueText.maxVisibleCharacters =
            totalCharacterCount;

        isTyping =
            false;

        SetNextDialogueIndicatorVisible(true);
    }


    private void ShowInstructionText(
        string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );

            typingCoroutine =
                null;
        }

        dialogueText.text =
            text;

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        isTyping =
            false;

        SetNextDialogueIndicatorVisible(false);
    }


    // =========================================================
    // 回転操作
    // =========================================================

    private void StartRotationExplanation()
    {
        PlayDialogueBlock(
            rotationInstructionDialogue,
            BeginWaitForRotation
        );
    }


    private void BeginWaitForRotation()
    {
        currentState =
            TutorialState.WaitForRotation;

        ShowMission(
            1,
            "潜望鏡をゆっくり左右に回せ"
        );

        accumulatedRotation =
            0f;

        if (sensorRead != null)
        {
            lastYaw =
                sensorRead.GetYaw();
        }
        else if (periscopeTransform != null)
        {
            lastYaw =
                periscopeTransform.eulerAngles.y;
        }
    }


    private void CheckPeriscopeRotation()
    {
        float currentYaw;

        if (sensorRead != null)
        {
            currentYaw =
                sensorRead.GetYaw();
        }
        else if (periscopeTransform != null)
        {
            currentYaw =
                periscopeTransform.eulerAngles.y;
        }
        else
        {
            return;
        }

        float delta =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    lastYaw,
                    currentYaw
                )
            );

        if (delta <=
            maxAcceptedYawDelta)
        {
            accumulatedRotation +=
                delta;
        }

        lastYaw =
            currentYaw;

        if (accumulatedRotation >=
            requiredRotationDegrees)
        {
            CompleteRotationStep();
        }
    }


    private void CompleteRotationStep()
    {
        PlayDialogueBlock(
            loweringInstructionDialogue,
            BeginWaitForLowering
        );
    }


    // =========================================================
    // 潜望鏡下降
    // =========================================================

    private void BeginWaitForLowering()
    {
        currentState =
            TutorialState.WaitForLowering;

        ShowMission(
            2,
            "つまみを右に回して潜望鏡を水中へ沈めろ"
        );
    }


    private void CheckPeriscopeLowering()
    {
        bool completed =
            false;

        if (periscopeTransform != null)
        {
            completed =
                periscopeTransform.position.y <=
                underwaterYThreshold;
        }
        else if (sensorRead != null)
        {
            completed =
                sensorRead.GetEncode() ==
                encoderValueForLowering;
        }

        if (completed)
        {
            CompleteLoweringStep();
        }
    }


    private void CompleteLoweringStep()
    {
        PlayDialogueBlock(
            sonarOpenInstructionDialogue,
            BeginWaitForSonarOpen
        );
    }


    // =========================================================
    // ソナー
    // =========================================================

    private void BeginWaitForSonarOpen()
    {
        currentState =
            TutorialState.WaitForSonarOpen;

        ShowMission(
            3,
            "スイッチを押し続けてソナーを表示せよ"
        );

        waitingForSonarButtonRelease =
            sensorRead != null &&
            sensorRead.GetTactileSwitch() == 1;

        if (menuPanelManager != null)
        {
            menuPanelManager.SetSonarInputEnabled(
                !waitingForSonarButtonRelease
            );
        }
    }


    private void CheckSonarOpened()
    {
        if (menuPanelManager == null)
        {
            return;
        }

        if (waitingForSonarButtonRelease)
        {
            bool tactileReleased =
                sensorRead == null ||
                sensorRead.GetTactileSwitch() == 0;

            bool spaceReleased =
                Keyboard.current == null ||
                !Keyboard.current.spaceKey.isPressed;

            if (tactileReleased &&
                spaceReleased)
            {
                waitingForSonarButtonRelease =
                    false;

                menuPanelManager.SetSonarInputEnabled(
                    true
                );
            }

            return;
        }

        if (!menuPanelManager.GetIsSonarPanelOpen())
        {
            return;
        }

        currentState =
            TutorialState.WaitForSonarClose;

        ShowMission(
            4,
            "周囲を確認し、スイッチから指を離せ"
        );

        ShowInstructionText(
            "ソナーの起動を確認した。\n" +
            "周囲にいる船のおおよその位置が表示されている。"
        );
    }


    private void CheckSonarClosed()
    {
        if (menuPanelManager == null)
        {
            return;
        }

        if (menuPanelManager.GetIsSonarPanelOpen())
        {
            return;
        }

        CompleteSonarStep();
    }


    private void CompleteSonarStep()
    {
        if (menuPanelManager != null)
        {
            menuPanelManager.SetSonarInputEnabled(false);
            menuPanelManager.CloseSonarPanel();
        }

        PlayDialogueBlock(
            sonarFinishedDialogue,
            BeginWaitForRaising
        );
    }


    // =========================================================
    // 潜望鏡上昇
    // =========================================================

    private void BeginWaitForRaising()
    {
        currentState =
            TutorialState.WaitForRaising;

        ShowMission(
            5,
            "つまみを左に回して潜望鏡を海上へ上げろ"
        );
    }


    private void CheckPeriscopeRaising()
    {
        bool completed =
            false;

        if (periscopeTransform != null)
        {
            completed =
                periscopeTransform.position.y >=
                raisedYThreshold;
        }
        else if (sensorRead != null)
        {
            completed =
                sensorRead.GetEncode() ==
                encoderValueForRaising;
        }

        if (completed)
        {
            CompleteRaisingStep();
        }
    }


    private void CompleteRaisingStep()
    {
        PlayDialogueBlock(
            enemySearchInstructionDialogue,
            BeginWaitForEnemyFound
        );
    }


    // =========================================================
    // 敵船発見
    // =========================================================

    private void BeginWaitForEnemyFound()
    {
        currentState =
            TutorialState.WaitForEnemyFound;

        ShowMission(
            6,
            "潜望鏡を回して敵船を発見せよ"
        );

        if (tutorialEnemyShip != null)
        {
            tutorialEnemyShip.SetDetectionEnabled(
                true
            );
        }
    }


    public void NotifyEnemyFound()
    {
        if (currentState !=
            TutorialState.WaitForEnemyFound)
        {
            return;
        }

        if (tutorialEnemyShip != null)
        {
            tutorialEnemyShip.SetDetectionEnabled(
                false
            );
        }

        PlayDialogueBlock(
            enemyFoundDialogue,
            BeginWaitForSignalFinished
        );
    }


    // =========================================================
    // 発光通信
    // =========================================================

    private void BeginWaitForSignalFinished()
    {
        currentState =
            TutorialState.WaitForSignalFinished;

        ShowMission(
            7,
            "敵船の光信号を観察し、順番を記憶せよ"
        );

        if (tutorialEnemyShip != null)
        {
            tutorialEnemyShip.StartSignal();
        }
        else
        {
            Debug.LogWarning(
                "TutorialEnemyShipが設定されていません。"
            );
        }
    }


    public void NotifyEnemySignalFinished()
    {
        if (currentState !=
            TutorialState.WaitForSignalFinished)
        {
            return;
        }

        PlayDialogueBlock(
            tutorialCompleteDialogue,
            BeginWaitForMissionStart
        );
    }


    // =========================================================
    // チュートリアル終了
    // =========================================================

    private void BeginWaitForMissionStart()
    {
        currentState =
            TutorialState.WaitForMissionStart;

        ShowMission(
            8,
            "スイッチを押して偵察任務を開始せよ"
        );

        ShowInstructionText(
            "準備ができたら、スイッチを押してください。"
        );
    }


    private void ChangeToMainScene()
    {
        if (isChangingScene)
        {
            return;
        }

        isChangingScene =
            true;

        SetNextDialogueIndicatorVisible(false);
        HideMission();

        currentState =
            TutorialState.Completed;

        SceneManager.LoadScene(
            mainSceneName
        );
    }


    // =========================================================
    // デバッグ
    // =========================================================

    private void DebugCompleteCurrentStep()
    {
        switch (currentState)
        {
            case TutorialState.Dialogue:

                if (isTyping)
                {
                    ShowAllCharacters();
                }
                else
                {
                    HandleDialogueAdvance();
                }

                break;


            case TutorialState.WaitForRotation:

                Debug.Log(
                    "デバッグ：潜望鏡の回転を成功扱いにしました。"
                );

                CompleteRotationStep();
                break;


            case TutorialState.WaitForLowering:

                Debug.Log(
                    "デバッグ：潜望鏡の下降を成功扱いにしました。"
                );

                SetPeriscopeYForDebug(
                    underwaterYThreshold -
                    0.1f
                );

                CompleteLoweringStep();
                break;


            case TutorialState.WaitForSonarOpen:
            case TutorialState.WaitForSonarClose:

                Debug.Log(
                    "デバッグ：ソナー操作を成功扱いにしました。"
                );

                CompleteSonarStep();
                break;


            case TutorialState.WaitForRaising:

                Debug.Log(
                    "デバッグ：潜望鏡の上昇を成功扱いにしました。"
                );

                SetPeriscopeYForDebug(
                    raisedYThreshold +
                    0.1f
                );

                CompleteRaisingStep();
                break;


            case TutorialState.WaitForEnemyFound:

                Debug.Log(
                    "デバッグ：敵船発見を成功扱いにしました。"
                );

                if (tutorialEnemyShip != null)
                {
                    tutorialEnemyShip.ForceDetectForDebug();
                }
                else
                {
                    NotifyEnemyFound();
                }

                break;


            case TutorialState.WaitForSignalFinished:

                Debug.Log(
                    "デバッグ：発光通信を終了扱いにしました。"
                );

                if (tutorialEnemyShip != null)
                {
                    tutorialEnemyShip.ForceFinishSignalForDebug();
                }
                else
                {
                    NotifyEnemySignalFinished();
                }

                break;


            case TutorialState.WaitForMissionStart:

                Debug.Log(
                    "デバッグ：MainSceneへ移動します。"
                );

                ChangeToMainScene();
                break;
        }
    }


    private void SetPeriscopeYForDebug(
        float newY)
    {
        if (periscopeTransform == null)
        {
            return;
        }

        Vector3 newPosition =
            periscopeTransform.position;

        newPosition.y =
            newY;

        Rigidbody periscopeRigidbody =
            periscopeTransform.GetComponent<Rigidbody>();

        if (periscopeRigidbody != null)
        {
            periscopeRigidbody.position =
                newPosition;

            periscopeRigidbody.linearVelocity =
                Vector3.zero;
        }
        else
        {
            periscopeTransform.position =
                newPosition;
        }

        DataManager.SetSubmarinePosition(
            newPosition
        );
    }
}