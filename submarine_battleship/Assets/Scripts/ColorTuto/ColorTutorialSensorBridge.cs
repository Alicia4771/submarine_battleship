using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class ColorTutorialSensorBridge : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "実機・キーボード入力を取得するSensorRead。" +
        "未設定の場合は自動検索する")]
    private SensorRead sensorRead;


    [SerializeField, Tooltip(
        "Yaw操作を有効にした瞬間の角度を基準にし直すためのPeriscopeController。" +
        "未設定の場合は自動検索する")]
    private PeriscopeController periscopeController;


    // ============================================================
    // Initialization
    // ============================================================

    [Header("Initialization")]

    [SerializeField, Tooltip(
        "ColorTutorialScene開始時にDataManagerを初期化する")]
    private bool initializeDataManagerOnAwake =
        true;


    [SerializeField, Tooltip(
        "Yaw操作をOFFからONへ切り替えたとき、" +
        "その時点のセンサ角度を新しい基準にして潜望鏡の急回転を防ぐ")]
    private bool recenterYawWhenEnabled =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Allowed Inputs
    // ============================================================

    private bool yawAllowed =
        false;


    private bool button1Allowed =
        false;


    private bool button2Allowed =
        false;


    private bool button3Allowed =
        false;


    private bool button4Allowed =
        false;


    private bool button6Allowed =
        false;


    // ============================================================
    // Internal
    // ============================================================

    private bool recenterYawOnNextUpdate =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        if (initializeDataManagerOnAwake)
        {
            DataManager.Initialize();
        }
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        SetAllTutorialInputsAllowed(
            false
        );
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (sensorRead == null)
        {
            ResolveReferences();

            if (sensorRead == null)
            {
                return;
            }
        }


        UpdateSensorData();
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (sensorRead == null)
        {
            sensorRead =
                FindFirstObjectByType<
                    SensorRead
                >();
        }


        if (periscopeController == null)
        {
            periscopeController =
                FindFirstObjectByType<
                    PeriscopeController
                >();
        }


        if (
            sensorRead == null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "ColorTutorialSensorBridge: " +
                "SensorReadが見つかりません。"
            );
        }


        if (
            periscopeController == null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "ColorTutorialSensorBridge: " +
                "PeriscopeControllerが見つかりません。"
            );
        }
    }


    // ============================================================
    // SensorRead → DataManager
    // ============================================================

    private void UpdateSensorData()
    {
        sensorRead.GetSensorData(
            out float yaw,
            out float speed,
            out int button1,
            out int button2,
            out int button3,
            out int button4,
            out int button5,
            out int button6
        );


        // ========================================================
        // Speed
        // ========================================================
        //
        // 現在のゲーム側との互換性のため、
        // Speedは常にDataManagerへ渡す。
        // ========================================================

        DataManager.SetSensorSpeed(
            speed
        );


        // ========================================================
        // Yaw
        // ========================================================
        //
        // Yawが許可されていないSTEPでは値を更新しない。
        // これにより、潜望鏡を回す練習STEP以外では
        // 実物を回してもゲーム内潜望鏡は回転しない。
        // ========================================================

        if (yawAllowed)
        {
            DataManager.SetSensorYaw(
                yaw
            );


            if (recenterYawOnNextUpdate)
            {
                recenterYawOnNextUpdate =
                    false;


                if (
                    recenterYawWhenEnabled &&
                    periscopeController != null
                )
                {
                    periscopeController
                        .RecenterYaw();
                }


                if (debugLog)
                {
                    Debug.Log(
                        "ColorTutorialSensorBridge: " +
                        "Yaw操作を有効化し、現在角度を基準化しました。"
                    );
                }
            }
        }


        // ========================================================
        // Buttons
        // ========================================================
        //
        // 許可されていないボタンは0としてDataManagerへ渡す。
        //
        // Button1 = 黒
        // Button2 = 赤
        // Button3 = 青
        // Button4 = 黄
        // Button6 = 白
        //
        // Button5はチュートリアルでは使用しない。
        // ========================================================

        DataManager.SetSensorButton1(
            button1Allowed
                ? button1
                : 0
        );


        DataManager.SetSensorButton2(
            button2Allowed
                ? button2
                : 0
        );


        DataManager.SetSensorButton3(
            button3Allowed
                ? button3
                : 0
        );


        DataManager.SetSensorButton4(
            button4Allowed
                ? button4
                : 0
        );


        DataManager.SetSensorButton5(
            0
        );


        DataManager.SetSensorButton6(
            button6Allowed
                ? button6
                : 0
        );
    }


    // ============================================================
    // All Inputs
    // ============================================================

    public void SetAllTutorialInputsAllowed(
        bool allowed
    )
    {
        yawAllowed =
            allowed;


        button1Allowed =
            allowed;


        button2Allowed =
            allowed;


        button3Allowed =
            allowed;


        button4Allowed =
            allowed;


        button6Allowed =
            allowed;


        if (
            allowed &&
            recenterYawWhenEnabled
        )
        {
            recenterYawOnNextUpdate =
                true;
        }
        else if (!allowed)
        {
            recenterYawOnNextUpdate =
                false;
        }
    }


    // ============================================================
    // All Buttons
    // ============================================================

    public void SetAllButtonsAllowed(
        bool allowed
    )
    {
        button1Allowed =
            allowed;


        button2Allowed =
            allowed;


        button3Allowed =
            allowed;


        button4Allowed =
            allowed;


        button6Allowed =
            allowed;
    }


    // ============================================================
    // Bulk Button Settings
    // ============================================================

    public void SetButtonsAllowed(
        bool allowButton1,
        bool allowButton2,
        bool allowButton3,
        bool allowButton4,
        bool allowButton6
    )
    {
        button1Allowed =
            allowButton1;


        button2Allowed =
            allowButton2;


        button3Allowed =
            allowButton3;


        button4Allowed =
            allowButton4;


        button6Allowed =
            allowButton6;
    }


    // ============================================================
    // Yaw
    // ============================================================

    public void SetYawAllowed(
        bool allowed
    )
    {
        if (
            yawAllowed ==
            allowed
        )
        {
            return;
        }


        yawAllowed =
            allowed;


        if (yawAllowed)
        {
            recenterYawOnNextUpdate =
                recenterYawWhenEnabled;
        }
        else
        {
            recenterYawOnNextUpdate =
                false;
        }


        if (debugLog)
        {
            Debug.Log(
                "ColorTutorialSensorBridge: Yaw = " +
                (
                    yawAllowed
                        ? "Allowed"
                        : "Blocked"
                )
            );
        }
    }


    // ============================================================
    // Individual Buttons
    // ============================================================

    public void SetButton1Allowed(
        bool allowed
    )
    {
        button1Allowed =
            allowed;
    }


    public void SetButton2Allowed(
        bool allowed
    )
    {
        button2Allowed =
            allowed;
    }


    public void SetButton3Allowed(
        bool allowed
    )
    {
        button3Allowed =
            allowed;
    }


    public void SetButton4Allowed(
        bool allowed
    )
    {
        button4Allowed =
            allowed;
    }


    public void SetButton6Allowed(
        bool allowed
    )
    {
        button6Allowed =
            allowed;
    }


    // ============================================================
    // Convenience
    // ============================================================

    public void AllowOnlyButton1()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        button1Allowed =
            true;
    }


    public void AllowOnlyButton2()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        button2Allowed =
            true;
    }


    public void AllowOnlyButton3()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        button3Allowed =
            true;
    }


    public void AllowOnlyButton4()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        button4Allowed =
            true;
    }


    public void AllowOnlyButton6()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        button6Allowed =
            true;
    }


    public void AllowOnlyYaw()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        SetYawAllowed(
            true
        );
    }


    public void AllowColorInputButtons()
    {
        SetAllTutorialInputsAllowed(
            false
        );


        button2Allowed =
            true;


        button3Allowed =
            true;


        button4Allowed =
            true;


        button6Allowed =
            true;
    }


    // ============================================================
    // Getters
    // ============================================================

    public bool GetYawAllowed()
    {
        return
            yawAllowed;
    }


    public bool GetButton1Allowed()
    {
        return
            button1Allowed;
    }


    public bool GetButton2Allowed()
    {
        return
            button2Allowed;
    }


    public bool GetButton3Allowed()
    {
        return
            button3Allowed;
    }


    public bool GetButton4Allowed()
    {
        return
            button4Allowed;
    }


    public bool GetButton6Allowed()
    {
        return
            button6Allowed;
    }
}
