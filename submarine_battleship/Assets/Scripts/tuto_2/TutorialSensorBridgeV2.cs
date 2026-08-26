using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class TutorialSensorBridgeV2 : MonoBehaviour
{
    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "センサ値を取得するSensorRead。" +
        "未設定の場合は自動検索する")]
    private SensorRead sensorRead;


    // ============================================================
    // 初期化
    // ============================================================

    [Header("Initialization")]

    [SerializeField, Tooltip(
        "TutorialScene開始時にDataManagerを初期化する")]
    private bool initializeDataManagerOnAwake =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Button許可状態
    // ============================================================

    private bool button1Allowed =
        false;

    private bool button2Allowed =
        false;

    private bool button3Allowed =
        false;

    private bool button4Allowed =
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
        ResolveSensorRead();

        SetAllGameplayButtonsAllowed(
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
            ResolveSensorRead();

            if (sensorRead == null)
            {
                return;
            }
        }


        UpdateSensorData();
    }


    // ============================================================
    // SensorRead取得
    // ============================================================

    private void ResolveSensorRead()
    {
        if (sensorRead != null)
        {
            return;
        }


        sensorRead =
            FindFirstObjectByType<SensorRead>();


        if (
            sensorRead == null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "TutorialSensorBridgeV2: " +
                "SensorReadが見つかりません。"
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
        // Yaw / Speed
        // ========================================================

        DataManager.SetSensorYaw(
            yaw
        );


        DataManager.SetSensorSpeed(
            speed
        );


        // ========================================================
        // Button1～4
        // ========================================================
        //
        // チュートリアルで許可されていない操作は
        // DataManagerへ0として渡す。
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


        // ========================================================
        // Button5 / Button6
        // ========================================================
        //
        // ゲームプレイでは使用しない。
        // ========================================================

        DataManager.SetSensorButton5(
            0
        );


        DataManager.SetSensorButton6(
            0
        );
    }


    // ============================================================
    // 全Button
    // ============================================================

    public void SetAllGameplayButtonsAllowed(
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
    }


    // ============================================================
    // 一括設定
    // ============================================================

    public void SetGameplayButtonsAllowed(
        bool allowButton1,
        bool allowButton2,
        bool allowButton3,
        bool allowButton4
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
    }


    // ============================================================
    // 個別設定
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


    // ============================================================
    // Getter
    // ============================================================

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
}