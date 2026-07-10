using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SensorRead : MonoBehaviour
{
    [Header("Serial Settings")]
    // private string portName = "/dev/cu.usbserial-140";      // eluq's Mac
    private string portName = "/dev/cu.usbserial-120";      // rin's Mac
    // private string portName = "/dev/cu.usbserial-1130";      // yuuya's Mac
    [SerializeField] private int baudRate = 115200;

    private string sensor_value = "0,0,0";
    private float yaw = 0f;
    private float speed = 0f;
    private int encodet = 0;

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
                sensor_value = serial.ReadLine();
                
                string[] values = sensor_value.Split(',');
                if (values.Length >= 3 && float.TryParse(values[0], out float yawValue) && float.TryParse(values[1], out float speedValue) && int.TryParse(values[2], out int encodeValue))
                {
                    lock (lockObj)
                    {
                        yaw = yawValue; // スレッドセーフに更新
                        speed = speedValue; // スレッドセーフに更新
                        encodet = encodeValue; // スレッドセーフに更新
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
        return yaw;
    }

    public float GetSpeed()
    {
        return speed;
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