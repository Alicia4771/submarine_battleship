using UnityEngine;
using UnityEngine.InputSystem;

public class ProconReceiver : MonoBehaviour
{
    [SerializeField] private MainCamera mainCamera;
    [SerializeField] private Submarine submarine;
    [SerializeField] private float mainCamera_rotation_speed_magnification;
    [SerializeField] private float submarine_rotation_speed_magnification;


    void Awake()
    {
        if (submarine_rotation_speed_magnification <= 0) submarine_rotation_speed_magnification = 1;
        if (mainCamera_rotation_speed_magnification <= 0) mainCamera_rotation_speed_magnification = 1;
    }

    void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        Vector2 leftStick = gamepad.leftStick.ReadValue();      // 左スティック(Vector2)
        Vector2 rightStick = gamepad.rightStick.ReadValue();    // 右スティック(Vector2)

        submarine.SetRotateSpeed(leftStick.x * submarine_rotation_speed_magnification);
        mainCamera.SetRotateSpeed(rightStick.x * mainCamera_rotation_speed_magnification);
    }
}
