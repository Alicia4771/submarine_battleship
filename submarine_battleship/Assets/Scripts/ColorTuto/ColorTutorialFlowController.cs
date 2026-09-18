using TMPro;
using UnityEngine;

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
        WaitForLeftRotation = 4,
        WaitForSignalTutorialSetup = 5
    }


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "チュートリアル専用のSensor Bridge")]
    private ColorTutorialSensorBridge
        sensorBridge;


    [SerializeField, Tooltip(
        "チュートリアル動画を切り替えるController")]
    private ColorTutorialVideoController
        videoController;


    [SerializeField, Tooltip(
        "Canvas内のSonarPanel")]
    private GameObject
        sonarPanel;


    [SerializeField, Tooltip(
        "STEP 1 / 6 などを表示するTextMeshProUGUI")]
    private TMP_Text
        stepText;


    [SerializeField, Tooltip(
        "各STEPの操作説明を表示するTextMeshProUGUI")]
    private TMP_Text
        instructionText;


    // ============================================================
    // Step 1 - Sonar
    // ============================================================

    [Header("Step 1 - Sonar")]

    [SerializeField, TextArea(2, 4)]
    private string sonarInstruction =
        "黒いボタンを押して\nソナーを表示してください。";


    [SerializeField, TextArea(2, 4)]
    private string sonarReleaseInstruction =
        "ソナーで周囲を確認したら、\n黒いボタンを離してください。";


    // ============================================================
    // Step 2 - Raise
    // ============================================================

    [Header("Step 2 - Raise Periscope")]

    [SerializeField, TextArea(2, 4)]
    private string raiseInstruction =
        "赤いボタンを押し続けて\n潜望鏡を海面上まで上げてください。";


    // ============================================================
    // Step 3 - Rotate
    // ============================================================

    [Header("Step 3 - Rotate Left")]

    [SerializeField, TextArea(2, 4)]
    private string rotateLeftInstruction =
        "潜望鏡を左へ回して\n周囲を確認してください。";


    [SerializeField, Min(1.0f), Tooltip(
        "STEP3完了に必要な左回転量")]
    private float requiredLeftRotationDegrees =
        30.0f;


    [SerializeField, Tooltip(
        "通常はON。左回転時にDataManagerのYawが減少する構成。" +
        "左へ回してもSTEPが進まない場合はOFFを試す")]
    private bool leftRotationIsNegative =
        true;


    [SerializeField, Min(0.0f), Tooltip(
        "1フレームでこれ以上変化したYawは異常値として無視する")]
    private float maximumAcceptedYawDelta =
        90.0f;


    // ============================================================
    // Step 4 Preview
    // ============================================================

    [Header("Step 4 - Signal Preview")]

    [SerializeField, TextArea(2, 4), Tooltip(
        "STEP3確認後に表示する文章。" +
        "次の実装で敵艦の固定信号開始処理を接続する")]
    private string signalPreparationInstruction =
        "次は敵艦の光信号を確認します。\n光った色の順番を覚えてください。";


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Internal
    // ============================================================

    [SerializeField]
    private TutorialState currentState =
        TutorialState.None;


    private float previousPeriscopeYaw =
        0.0f;


    private float accumulatedLeftRotation =
        0.0f;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        if (sensorBridge == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ColorTutorialSensorBridgeが見つかりません。"
            );

            enabled =
                false;

            return;
        }


        if (videoController == null)
        {
            Debug.LogError(
                "ColorTutorialFlowController: " +
                "ColorTutorialVideoControllerが見つかりません。"
            );

            enabled =
                false;

            return;
        }


        sensorBridge
            .SetAllTutorialInputsAllowed(
                false
            );


        BeginStep1Sonar();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
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


            case TutorialState.WaitForLeftRotation:

                CheckLeftRotation();

                break;


            case TutorialState.WaitForSignalTutorialSetup:
            case TutorialState.None:
            default:

                break;
        }
    }


    // ============================================================
    // Resolve
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
    }


    // ============================================================
    // Step 1 - Sonar
    // ============================================================

    private void BeginStep1Sonar()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyButton1();


        currentState =
            TutorialState.WaitForSonarOpen;


        SetTutorialText(
            1,
            sonarInstruction
        );


        videoController
            .PlaySonarVideo();


        DebugMessage(
            "STEP1開始: ソナー"
        );
    }


    private void CheckSonarOpen()
    {
        // SonarPanelそのものが割り当てられている場合は、
        // 実際にPanelが開いたことを確認する。
        if (sonarPanel != null)
        {
            if (!sonarPanel.activeSelf)
            {
                return;
            }
        }
        else
        {
            // SonarPanelが未設定の場合はButton1押下を完了条件にする。
            if (
                DataManager
                    .GetSensorButton1()
                !=
                1
            )
            {
                return;
            }
        }


        currentState =
            TutorialState.WaitForSonarRelease;


        SetTutorialText(
            1,
            sonarReleaseInstruction
        );


        DebugMessage(
            "STEP1: ソナー表示確認"
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


        // SonarPanelが閉じるのは別ComponentのUpdateになるため、
        // Button1が離されたことを基準に次へ進む。
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
            TutorialState
                .WaitForPeriscopeRaised;


        SetTutorialText(
            2,
            raiseInstruction
        );


        videoController
            .PlayRaisePeriscopeVideo();


        DebugMessage(
            "STEP2開始: 潜望鏡上昇"
        );
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


        BeginStep3RotateLeft();
    }


    // ============================================================
    // Step 3 - Rotate Left
    // ============================================================

    private void BeginStep3RotateLeft()
    {
        DisableAllInputs();


        sensorBridge
            .AllowOnlyYaw();


        accumulatedLeftRotation =
            0.0f;


        previousPeriscopeYaw =
            DataManager
                .GetPeriscopeRotation();


        currentState =
            TutorialState
                .WaitForLeftRotation;


        SetTutorialText(
            3,
            rotateLeftInstruction
        );


        videoController
            .PlayRotateLeftVideo();


        DebugMessage(
            "STEP3開始: 左回転"
        );
    }


    private void CheckLeftRotation()
    {
        float currentYaw =
            DataManager
                .GetPeriscopeRotation();


        float delta =
            Mathf.DeltaAngle(
                previousPeriscopeYaw,
                currentYaw
            );


        previousPeriscopeYaw =
            currentYaw;


        if (
            Mathf.Abs(delta) >
            maximumAcceptedYawDelta
        )
        {
            return;
        }


        float leftDelta =
            leftRotationIsNegative
                ? -delta
                : delta;


        if (leftDelta > 0.0f)
        {
            accumulatedLeftRotation +=
                leftDelta;
        }


        if (
            accumulatedLeftRotation <
            requiredLeftRotationDegrees
        )
        {
            return;
        }


        BeginStep4SignalPreparation();
    }


    // ============================================================
    // Step 4 - Signal Preparation
    // ============================================================

    private void BeginStep4SignalPreparation()
    {
        DisableAllInputs();


        videoController
            .HideVideo();


        currentState =
            TutorialState
                .WaitForSignalTutorialSetup;


        SetTutorialText(
            4,
            signalPreparationInstruction
        );


        DebugMessage(
            "STEP3完了。次は固定色信号の実装へ進みます。"
        );
    }


    // ============================================================
    // Input
    // ============================================================

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


    // ============================================================
    // UI
    // ============================================================

    private void SetTutorialText(
        int step,
        string instruction
    )
    {
        if (stepText != null)
        {
            stepText.text =
                "STEP " +
                step +
                " / 6";
        }


        if (instructionText != null)
        {
            instructionText.text =
                instruction;
        }
    }


    // ============================================================
    // Getter
    // ============================================================

    public int GetCurrentStepForDebug()
    {
        switch (currentState)
        {
            case TutorialState.WaitForSonarOpen:
            case TutorialState.WaitForSonarRelease:
                return 1;

            case TutorialState.WaitForPeriscopeRaised:
                return 2;

            case TutorialState.WaitForLeftRotation:
                return 3;

            case TutorialState.WaitForSignalTutorialSetup:
                return 4;

            default:
                return 0;
        }
    }


    public float GetAccumulatedLeftRotationForDebug()
    {
        return
            accumulatedLeftRotation;
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
            "ColorTutorialFlowController: " +
            message
        );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        requiredLeftRotationDegrees =
            Mathf.Max(
                1.0f,
                requiredLeftRotationDegrees
            );


        maximumAcceptedYawDelta =
            Mathf.Max(
                0.0f,
                maximumAcceptedYawDelta
            );


        if (sonarInstruction == null)
        {
            sonarInstruction =
                string.Empty;
        }


        if (sonarReleaseInstruction == null)
        {
            sonarReleaseInstruction =
                string.Empty;
        }


        if (raiseInstruction == null)
        {
            raiseInstruction =
                string.Empty;
        }


        if (rotateLeftInstruction == null)
        {
            rotateLeftInstruction =
                string.Empty;
        }


        if (signalPreparationInstruction == null)
        {
            signalPreparationInstruction =
                string.Empty;
        }
    }
}
