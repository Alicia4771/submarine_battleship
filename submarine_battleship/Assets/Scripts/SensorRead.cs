using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SensorRead : MonoBehaviour
{
    [Header("Serial Settings")]
    // [SerializeField] private string portName = "/dev/cu.usbserial-1140";
    private string portName = "/dev/cu.usbserial-120";      // rin's Mac
    [SerializeField] private int baudRate = 115200;

    private float yaw = 0f;

    private SerialPort serial;
    private Thread readThread;
    private bool running = false;
    private object lockObj = new object(); // スレッド安全用

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
            Debug.LogError("SerialPort Open Failed: " + e.Message);
            return;
        }

        running = true;
        readThread = new Thread(ReadSerialLoop);
        readThread.IsBackground = true;
        readThread.Start();
    }

    private void ReadSerialLoop()
    {
        while (running && serial != null && serial.IsOpen)
        {
            try
            {
                string line = serial.ReadLine();
                if (float.TryParse(line, out float value))
                {
                    lock (lockObj)
                    {
                        yaw = value; // スレッドセーフに更新
                    }
                }
            }
            catch (System.TimeoutException)
            {
                // タイムアウトは無視
            }
            catch (System.Exception e)
            {
                Debug.LogError("Serial Read Error: " + e.Message);
            }
        }
    }

    public float GetYaw()
    {
        // メインスレッドで安全に読み取りたい場合
        lock (lockObj)
        {
            return yaw;
        }
    }

    void OnDestroy()
    {
        running = false;
        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(); // スレッド終了待ち
        }

        if (serial != null && serial.IsOpen)
        {
            serial.Close();
        }
    }
}
