using UnityEngine;
using UnityEngine.InputSystem;
using System.IO.Ports;
using System.Threading;

public class SensorRead : MonoBehaviour
{
    // =========================
    // 定数
    // =========================

    private const int ButtonReleased = 0;
    private const int ButtonPressed = 1;
    private const float FullCircleDegrees = 360.0f;


    // =========================
    // シリアル通信設定
    // =========================

    [Header("Serial Settings")]

    // private string portName = "/dev/cu.usbserial-140";      // eluq's Mac
    private string portName = "/dev/cu.usbserial-110";         // rin's Mac
    // private string portName = "/dev/cu.usbserial-1130";     // yuuya's Mac

    [SerializeField]
    private int baudRate = 115200;


    // =========================
    // キーボードButtonシミュレーション
    // =========================

    [Header("Keyboard Button Simulation")]

    [SerializeField, Tooltip(
        "ONの場合、キーボードの数字キー1～6をButton1～6として使用できる")]
    private bool enableKeyboardButtonSimulation = true;


    [SerializeField, Tooltip(
        "ONの場合、テンキーの1～6もButton1～6として使用できる")]
    private bool enableNumpadButtonSimulation = true;


    // =========================
    // キーボードYawシミュレーション
    // =========================

    [Header("Keyboard Yaw Simulation")]

    [SerializeField, Tooltip(
        "ONの場合、A/Dキーで9軸センサのYaw回転をシミュレーションする。" +
        "A = 反時計回り、D = 時計回り")]
    private bool enableKeyboardYawSimulation = true;


    [SerializeField, Min(0.0f), Tooltip(
        "A/Dキーを押している間に1秒あたり何度Yawを変化させるか")]
    private float keyboardYawDegreesPerSecond = 90.0f;


    // =========================
    // デバッグ設定
    // =========================

    [Header("Debug Settings")]

    [SerializeField]
    private bool debugLog = false;


    // =========================
    // センサー値
    // =========================

    /*
     * ラズパイから受信する形式
     *
     * yaw,speed,button1,button2,button3,button4,button5,button6
     *
     * 例
     *
     * 125.4,-2.3,1,0,0,1,0,1
     */

    private string sensor_value = "0,0,0,0,0,0,0,0";

    private float yaw = 0f;
    private float speed = 0f;


    // =========================
    // Keyboard Yaw
    // =========================

    // 実機Yawへ加算するキーボード操作分
    private float keyboardYawOffset = 0.0f;


    // =========================
    // Raspberry Piから受信した実機Button値
    // =========================

    private int button1 = 0;
    private int button2 = 0;
    private int button3 = 0;
    private int button4 = 0;
    private int button5 = 0;
    private int button6 = 0;


    // =========================
    // デバッグ表示用
    // =========================

    private string debugMessage = "";
    private bool hasDebugMessage = false;


    // =========================
    // シリアル通信用
    // =========================

    private SerialPort serial;
    private Thread readThread;

    private bool running = false;

    // スレッド安全用
    private readonly object lockObj = new object();


    // =========================
    // Start
    // =========================

    void Start()
    {
        serial = new SerialPort(
            portName,
            baudRate
        );

        serial.ReadTimeout = 50;


        try
        {
            serial.Open();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "SerialPort Open Failed: " +
                e.Message +
                "\n" +
                "実機からの入力は使用できません。" +
                "Keyboard SimulationがONの場合はキーボード入力を使用できます。"
            );

            // シリアル通信だけ停止する。
            // UpdateやGetterは引き続き動作するため、
            // 実機がなくてもキーボード入力を使用できる。
            return;
        }


        // シリアル受信スレッド開始
        running = true;

        readThread = new Thread(
            ReadSerialLoop
        );

        readThread.IsBackground = true;

        readThread.Start();
    }


    // =========================
    // Update
    // =========================

    void Update()
    {
        // A/DキーによるYawシミュレーションは
        // debugLogのON/OFFに関係なく毎フレーム更新する。
        UpdateKeyboardYawSimulation();


        // デバッグ表示がOFFなら表示しない
        if (!debugLog)
        {
            lock (lockObj)
            {
                hasDebugMessage = false;
            }

            return;
        }


        string message = null;


        lock (lockObj)
        {
            if (hasDebugMessage)
            {
                message = debugMessage;
                hasDebugMessage = false;
            }
        }


        // Debug.LogはUnityのメインスレッドから実行
        if (message != null)
        {
            Debug.Log(
                message
            );
        }
    }


    // =========================
    // Keyboard Yaw更新
    // =========================

    private void UpdateKeyboardYawSimulation()
    {
        if (!enableKeyboardYawSimulation)
        {
            keyboardYawOffset = 0.0f;
            return;
        }


        if (Keyboard.current == null)
        {
            return;
        }


        bool aPressed =
            Keyboard.current
                .aKey
                .isPressed;


        bool dPressed =
            Keyboard.current
                .dKey
                .isPressed;


        // AとDを同時押し、または両方未押下なら回転しない
        if (aPressed == dPressed)
        {
            return;
        }


        float direction =
            dPressed
                ? 1.0f
                : -1.0f;


        keyboardYawOffset +=
            direction *
            keyboardYawDegreesPerSecond *
            Time.deltaTime;


        // 0～360度に正規化
        keyboardYawOffset =
            Mathf.Repeat(
                keyboardYawOffset,
                FullCircleDegrees
            );
    }


    // =========================
    // シリアル受信
    // =========================

    private void ReadSerialLoop()
    {
        while (
            running &&
            serial != null &&
            serial.IsOpen
        )
        {
            try
            {
                // 1行読み込む
                //
                // yaw,speed,button1,button2,button3,button4,button5,button6

                sensor_value = serial.ReadLine();


                // カンマ区切りで分割
                string[] values = sensor_value.Split(',');


                // 必要な8個の値が存在するか確認
                if (
                    values.Length >= 8 &&

                    float.TryParse(
                        values[0],
                        out float yawValue
                    ) &&

                    float.TryParse(
                        values[1],
                        out float speedValue
                    ) &&

                    int.TryParse(
                        values[2],
                        out int button1Value
                    ) &&

                    int.TryParse(
                        values[3],
                        out int button2Value
                    ) &&

                    int.TryParse(
                        values[4],
                        out int button3Value
                    ) &&

                    int.TryParse(
                        values[5],
                        out int button4Value
                    ) &&

                    int.TryParse(
                        values[6],
                        out int button5Value
                    ) &&

                    int.TryParse(
                        values[7],
                        out int button6Value
                    )
                )
                {
                    lock (lockObj)
                    {
                        // =========================
                        // 最新値を保存
                        // =========================

                        yaw = yawValue;
                        speed = speedValue;

                        button1 = button1Value;
                        button2 = button2Value;
                        button3 = button3Value;
                        button4 = button4Value;
                        button5 = button5Value;
                        button6 = button6Value;


                        // =========================
                        // デバッグ表示用データ
                        // =========================

                        if (debugLog)
                        {
                            debugMessage =
                                yawValue + "," +
                                speedValue + "," +
                                button1Value + "," +
                                button2Value + "," +
                                button3Value + "," +
                                button4Value + "," +
                                button5Value + "," +
                                button6Value;

                            hasDebugMessage = true;
                        }
                    }
                }
            }
            catch (System.TimeoutException)
            {
                // タイムアウトは無視
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    "Serial Read Error: " +
                    e.Message
                );
            }
        }
    }


    // =========================
    // 全センサー値をまとめて取得
    // =========================

    public void GetSensorData(
        out float yawValue,
        out float speedValue,
        out int button1Value,
        out int button2Value,
        out int button3Value,
        out int button4Value,
        out int button5Value,
        out int button6Value
    )
    {
        float serialYaw;

        int serialButton1;
        int serialButton2;
        int serialButton3;
        int serialButton4;
        int serialButton5;
        int serialButton6;


        lock (lockObj)
        {
            serialYaw = yaw;
            speedValue = speed;

            serialButton1 = button1;
            serialButton2 = button2;
            serialButton3 = button3;
            serialButton4 = button4;
            serialButton5 = button5;
            serialButton6 = button6;
        }


        // =========================
        // 実機Yaw + Keyboard Yaw
        // =========================

        yawValue =
            GetCombinedYaw(
                serialYaw
            );


        // =========================
        // 実機Button OR Keyboard
        // =========================

        button1Value =
            CombineButtonInput(
                serialButton1,
                GetKeyboardButton1()
            );

        button2Value =
            CombineButtonInput(
                serialButton2,
                GetKeyboardButton2()
            );

        button3Value =
            CombineButtonInput(
                serialButton3,
                GetKeyboardButton3()
            );

        button4Value =
            CombineButtonInput(
                serialButton4,
                GetKeyboardButton4()
            );

        button5Value =
            CombineButtonInput(
                serialButton5,
                GetKeyboardButton5()
            );

        button6Value =
            CombineButtonInput(
                serialButton6,
                GetKeyboardButton6()
            );
    }


    // =========================
    // Yaw取得
    // =========================

    public float GetYaw()
    {
        float serialYaw;


        lock (lockObj)
        {
            serialYaw = yaw;
        }


        return
            GetCombinedYaw(
                serialYaw
            );
    }


    // =========================
    // 実機Yaw + Keyboard Yaw
    // =========================

    private float GetCombinedYaw(
        float serialYaw
    )
    {
        if (!enableKeyboardYawSimulation)
        {
            return serialYaw;
        }


        return
            Mathf.Repeat(
                serialYaw +
                keyboardYawOffset,
                FullCircleDegrees
            );
    }


    // =========================
    // Speed取得
    // =========================

    public float GetSpeed()
    {
        lock (lockObj)
        {
            return speed;
        }
    }


    // =========================
    // Button1取得
    // =========================

    public int GetButton1()
    {
        int serialValue;

        lock (lockObj)
        {
            serialValue = button1;
        }

        return
            CombineButtonInput(
                serialValue,
                GetKeyboardButton1()
            );
    }


    // =========================
    // Button2取得
    // =========================

    public int GetButton2()
    {
        int serialValue;

        lock (lockObj)
        {
            serialValue = button2;
        }

        return
            CombineButtonInput(
                serialValue,
                GetKeyboardButton2()
            );
    }


    // =========================
    // Button3取得
    // =========================

    public int GetButton3()
    {
        int serialValue;

        lock (lockObj)
        {
            serialValue = button3;
        }

        return
            CombineButtonInput(
                serialValue,
                GetKeyboardButton3()
            );
    }


    // =========================
    // Button4取得
    // =========================

    public int GetButton4()
    {
        int serialValue;

        lock (lockObj)
        {
            serialValue = button4;
        }

        return
            CombineButtonInput(
                serialValue,
                GetKeyboardButton4()
            );
    }


    // =========================
    // Button5取得
    // =========================

    public int GetButton5()
    {
        int serialValue;

        lock (lockObj)
        {
            serialValue = button5;
        }

        return
            CombineButtonInput(
                serialValue,
                GetKeyboardButton5()
            );
    }


    // =========================
    // Button6取得
    // =========================

    public int GetButton6()
    {
        int serialValue;

        lock (lockObj)
        {
            serialValue = button6;
        }

        return
            CombineButtonInput(
                serialValue,
                GetKeyboardButton6()
            );
    }


    // =========================
    // 実機 + Keyboard Button
    // =========================

    private int CombineButtonInput(
        int serialValue,
        int keyboardValue
    )
    {
        if (
            serialValue == ButtonPressed ||
            keyboardValue == ButtonPressed
        )
        {
            return ButtonPressed;
        }


        return ButtonReleased;
    }


    // =========================
    // Keyboard Button1
    // =========================

    private int GetKeyboardButton1()
    {
        if (!CanUseKeyboardButtonSimulation())
        {
            return ButtonReleased;
        }


        bool pressed =
            Keyboard.current
                .digit1Key
                .isPressed;


        if (enableNumpadButtonSimulation)
        {
            pressed =
                pressed ||
                Keyboard.current
                    .numpad1Key
                    .isPressed;
        }


        return
            pressed
                ? ButtonPressed
                : ButtonReleased;
    }


    // =========================
    // Keyboard Button2
    // =========================

    private int GetKeyboardButton2()
    {
        if (!CanUseKeyboardButtonSimulation())
        {
            return ButtonReleased;
        }


        bool pressed =
            Keyboard.current
                .digit2Key
                .isPressed;


        if (enableNumpadButtonSimulation)
        {
            pressed =
                pressed ||
                Keyboard.current
                    .numpad2Key
                    .isPressed;
        }


        return
            pressed
                ? ButtonPressed
                : ButtonReleased;
    }


    // =========================
    // Keyboard Button3
    // =========================

    private int GetKeyboardButton3()
    {
        if (!CanUseKeyboardButtonSimulation())
        {
            return ButtonReleased;
        }


        bool pressed =
            Keyboard.current
                .digit3Key
                .isPressed;


        if (enableNumpadButtonSimulation)
        {
            pressed =
                pressed ||
                Keyboard.current
                    .numpad3Key
                    .isPressed;
        }


        return
            pressed
                ? ButtonPressed
                : ButtonReleased;
    }


    // =========================
    // Keyboard Button4
    // =========================

    private int GetKeyboardButton4()
    {
        if (!CanUseKeyboardButtonSimulation())
        {
            return ButtonReleased;
        }


        bool pressed =
            Keyboard.current
                .digit4Key
                .isPressed;


        if (enableNumpadButtonSimulation)
        {
            pressed =
                pressed ||
                Keyboard.current
                    .numpad4Key
                    .isPressed;
        }


        return
            pressed
                ? ButtonPressed
                : ButtonReleased;
    }


    // =========================
    // Keyboard Button5
    // =========================

    private int GetKeyboardButton5()
    {
        if (!CanUseKeyboardButtonSimulation())
        {
            return ButtonReleased;
        }


        bool pressed =
            Keyboard.current
                .digit5Key
                .isPressed;


        if (enableNumpadButtonSimulation)
        {
            pressed =
                pressed ||
                Keyboard.current
                    .numpad5Key
                    .isPressed;
        }


        return
            pressed
                ? ButtonPressed
                : ButtonReleased;
    }


    // =========================
    // Keyboard Button6
    // =========================

    private int GetKeyboardButton6()
    {
        if (!CanUseKeyboardButtonSimulation())
        {
            return ButtonReleased;
        }


        bool pressed =
            Keyboard.current
                .digit6Key
                .isPressed;


        if (enableNumpadButtonSimulation)
        {
            pressed =
                pressed ||
                Keyboard.current
                    .numpad6Key
                    .isPressed;
        }


        return
            pressed
                ? ButtonPressed
                : ButtonReleased;
    }


    // =========================
    // Keyboard Buttonを使用できるか
    // =========================

    private bool CanUseKeyboardButtonSimulation()
    {
        return
            enableKeyboardButtonSimulation &&
            Keyboard.current != null;
    }


    // =========================
    // Inspector
    // =========================

    private void OnValidate()
    {
        baudRate =
            Mathf.Max(
                1,
                baudRate
            );


        keyboardYawDegreesPerSecond =
            Mathf.Max(
                0.0f,
                keyboardYawDegreesPerSecond
            );
    }


    // =========================
    // 終了処理
    // =========================

    void OnDestroy()
    {
        running = false;


        if (
            readThread != null &&
            readThread.IsAlive
        )
        {
            // スレッド終了待ち
            readThread.Join();
        }


        if (
            serial != null &&
            serial.IsOpen
        )
        {
            serial.Close();
        }
    }
}