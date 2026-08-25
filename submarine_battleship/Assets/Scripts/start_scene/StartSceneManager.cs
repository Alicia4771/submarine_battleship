using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [SerializeField, Tooltip("センサ値を受信するSensorRead")]
    private SensorRead sensorRead;


    // 前フレームのボタン1の値
    private int previousButton1 = 0;


    // シーン移動を複数回実行しないためのフラグ
    private bool isChangingScene = false;


    // =========================
    // Start
    // =========================

    private void Start()
    {
        // SensorReadがInspectorで設定されていない場合は自動取得
        if (sensorRead == null)
        {
            sensorRead = FindFirstObjectByType<SensorRead>();
        }


        if (sensorRead != null)
        {
            // 現在のButton1の状態を初期値として保存
            previousButton1 = sensorRead.GetButton1();
        }
        else
        {
            Debug.LogWarning(
                "SensorReadが見つかりません。"
            );
        }
    }


    // =========================
    // Update
    // =========================

    private void Update()
    {
        CheckButton1();

        CheckKeyboardAndGamepad();
    }


    // =========================
    // Button1チェック
    // =========================

    private void CheckButton1()
    {
        if (
            sensorRead == null ||
            isChangingScene
        )
        {
            return;
        }


        // 現在のButton1の状態を取得
        int currentButton1 =
            sensorRead.GetButton1();


        // =========================
        // 0 → 1 になった瞬間だけ反応
        // =========================

        if (
            currentButton1 == 1 &&
            previousButton1 != 1
        )
        {
            ChangeToTutorialScene();
        }


        // 今回の値を次フレーム用に保存
        previousButton1 = currentButton1;
    }


    // =========================
    // キーボード・ゲームパッド
    // =========================

    private void CheckKeyboardAndGamepad()
    {
        if (isChangingScene)
        {
            return;
        }


        // Enterキー
        bool enterPressed =
            Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame;


        // ゲームパッド
        Gamepad gamepad =
            Gamepad.current;


        bool gamepadPressed =
            gamepad != null &&
            gamepad.buttonEast.wasPressedThisFrame;


        // どちらかが押されたらTutorialSceneへ
        if (
            enterPressed ||
            gamepadPressed
        )
        {
            ChangeToTutorialScene();
        }
    }


    // =========================
    // TutorialSceneへ移動
    // =========================

    private void ChangeToTutorialScene()
    {
        if (isChangingScene)
        {
            return;
        }


        isChangingScene = true;


        SceneManager.LoadScene(
            "TutorialScene"
        );
    }
}