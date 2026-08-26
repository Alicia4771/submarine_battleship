using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "センサ値を受信するSensorRead")]
    private SensorRead sensorRead;


    // ============================================================
    // Scene
    // ============================================================

    [Header("Scene")]

    [SerializeField, Tooltip(
        "チュートリアル選択シーン名")]
    private string tutorialChooseSceneName =
        "Tuto_choose";


    // ============================================================
    // Internal
    // ============================================================

    // 前フレームのボタン1
    private int previousButton1 =
        0;


    // シーン遷移の多重実行防止
    private bool isChangingScene =
        false;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        Time.timeScale =
            1.0f;


        // ========================================================
        // SensorRead自動取得
        // ========================================================

        if (sensorRead == null)
        {
            sensorRead =
                FindFirstObjectByType<
                    SensorRead
                >();
        }


        if (sensorRead != null)
        {
            previousButton1 =
                sensorRead.GetButton1();
        }
        else
        {
            Debug.LogWarning(
                "StartSceneManager: " +
                "SensorReadが見つかりません。"
            );
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (isChangingScene)
        {
            return;
        }


        CheckButton1();

        CheckKeyboardAndGamepad();
    }


    // ============================================================
    // 黒いボタン
    // ============================================================

    private void CheckButton1()
    {
        if (sensorRead == null)
        {
            return;
        }


        int currentButton1 =
            sensorRead.GetButton1();


        // ========================================================
        // 0 → 1になった瞬間のみ反応
        // ========================================================

        if (
            currentButton1 == 1 &&
            previousButton1 != 1
        )
        {
            ChangeToTutorialChooseScene();
        }


        previousButton1 =
            currentButton1;
    }


    // ============================================================
    // Keyboard / Gamepad
    // ============================================================

    private void CheckKeyboardAndGamepad()
    {
        bool enterPressed =
            Keyboard.current != null &&
            (
                Keyboard.current
                    .enterKey
                    .wasPressedThisFrame
                ||
                Keyboard.current
                    .numpadEnterKey
                    .wasPressedThisFrame
            );


        Gamepad gamepad =
            Gamepad.current;


        bool gamepadPressed =
            gamepad != null &&
            gamepad
                .buttonEast
                .wasPressedThisFrame;


        if (
            enterPressed ||
            gamepadPressed
        )
        {
            ChangeToTutorialChooseScene();
        }
    }


    // ============================================================
    // Tutorial Choose Scene
    // ============================================================

    private void ChangeToTutorialChooseScene()
    {
        if (isChangingScene)
        {
            return;
        }


        isChangingScene =
            true;


        Time.timeScale =
            1.0f;


        SceneManager.LoadScene(
            tutorialChooseSceneName
        );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        if (
            string.IsNullOrWhiteSpace(
                tutorialChooseSceneName
            )
        )
        {
            tutorialChooseSceneName =
                "Tuto_choose";
        }
    }
}