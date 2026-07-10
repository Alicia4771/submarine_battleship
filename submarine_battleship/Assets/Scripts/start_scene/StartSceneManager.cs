using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [SerializeField, Tooltip("センサ値を受信するSensorRead")]
    private SensorRead sensorRead;

    // 前フレームのタクトスイッチの値
    private int previousTactileSwitch = 0;

    // シーン移動を複数回実行しないためのフラグ
    private bool isChangingScene = false;

    private void Start()
    {
        if (sensorRead == null)
        {
            sensorRead = FindFirstObjectByType<SensorRead>();
        }

        if (sensorRead != null)
        {
            previousTactileSwitch = sensorRead.GetTactileSwitch();
        }
        else
        {
            Debug.LogWarning("SensorReadが見つかりません。");
        }
    }

    private void Update()
    {
        CheckTactileSwitch();
        CheckKeyboardAndGamepad();
    }

    private void CheckTactileSwitch()
    {
        if (sensorRead == null || isChangingScene)
        {
            return;
        }

        int currentTactileSwitch = sensorRead.GetTactileSwitch();

        // タクトスイッチが0から1になった瞬間
        if (currentTactileSwitch == 1 &&
            previousTactileSwitch != 1)
        {
            ChangeToTutorialScene();
        }

        previousTactileSwitch = currentTactileSwitch;
    }

    private void CheckKeyboardAndGamepad()
    {
        if (isChangingScene)
        {
            return;
        }

        bool enterPressed =
            Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame;

        Gamepad gamepad = Gamepad.current;

        bool gamepadPressed =
            gamepad != null &&
            gamepad.buttonEast.wasPressedThisFrame;

        if (enterPressed || gamepadPressed)
        {
            ChangeToTutorialScene();
        }
    }

    private void ChangeToTutorialScene()
    {
        if (isChangingScene)
        {
            return;
        }

        isChangingScene = true;
        SceneManager.LoadScene("TutorialScene");
    }
}