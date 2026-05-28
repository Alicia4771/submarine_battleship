using UnityEngine;
using UnityEngine.InputSystem;

public class ProconReceiver : MonoBehaviour
{
    [SerializeField] private MainCamera mainCamera;
    [SerializeField] private float rotation_speed_magnification;

    void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        Vector2 leftStick = gamepad.leftStick.ReadValue();      // 左スティック(Vector2)
        Vector2 rightStick = gamepad.rightStick.ReadValue();    // 右スティック(Vector2)

        mainCamera.SetRotateSpeed(leftStick.x * rotation_speed_magnification);
    }
}
