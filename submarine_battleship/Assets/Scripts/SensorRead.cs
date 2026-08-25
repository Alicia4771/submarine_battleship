using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SensorRead : MonoBehaviour
{
    // =========================
    // シリアル通信設定
    // =========================

    [Header("Serial Settings")]

    // private string portName = "/dev/cu.usbserial-140";      // eluq's Mac
    private string portName = "/dev/cu.usbserial-110";         // rin's Mac
    // private string portName = "/dev/cu.usbserial-1130";     // yuuya's Mac

    [SerializeField] private int baudRate = 115200;


    // =========================
    // デバッグ設定
    // =========================

    [Header("Debug Settings")]
    [SerializeField] private bool debugLog = false;


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
        serial = new SerialPort(portName, baudRate);

        serial.ReadTimeout = 50;


        try
        {
            serial.Open();
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "SerialPort Open Failed: " + e.Message
            );

            return;
        }


        // シリアル受信スレッド開始
        running = true;

        readThread = new Thread(ReadSerialLoop);

        readThread.IsBackground = true;

        readThread.Start();
    }


    // =========================
    // Update
    // =========================

    void Update()
    {
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
            Debug.Log(message);
        }
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
                    "Serial Read Error: " + e.Message
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
        lock (lockObj)
        {
            yawValue = yaw;
            speedValue = speed;

            button1Value = button1;
            button2Value = button2;
            button3Value = button3;
            button4Value = button4;
            button5Value = button5;
            button6Value = button6;
        }
    }


    // =========================
    // Yaw取得
    // =========================

    public float GetYaw()
    {
        lock (lockObj)
        {
            return yaw;
        }
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
        lock (lockObj)
        {
            return button1;
        }
    }


    // =========================
    // Button2取得
    // =========================

    public int GetButton2()
    {
        lock (lockObj)
        {
            return button2;
        }
    }


    // =========================
    // Button3取得
    // =========================

    public int GetButton3()
    {
        lock (lockObj)
        {
            return button3;
        }
    }


    // =========================
    // Button4取得
    // =========================

    public int GetButton4()
    {
        lock (lockObj)
        {
            return button4;
        }
    }


    // =========================
    // Button5取得
    // =========================

    public int GetButton5()
    {
        lock (lockObj)
        {
            return button5;
        }
    }


    // =========================
    // Button6取得
    // =========================

    public int GetButton6()
    {
        lock (lockObj)
        {
            return button6;
        }
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