using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ResultSceneManager : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const string StartSceneName =
        "StartScene";

    private const float DefaultAutoReturnTime =
        10.0f;

    private const float MinimumNonNegativeValue =
        0.0f;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "スコアを表示するTMPのテキスト")]
    private TMP_Text scoreText;


    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "ボタン入力を受信するSensorRead。" +
        "未設定の場合は自動検索する")]
    private SensorRead sensorRead;


    // ============================================================
    // Scene Transition
    // ============================================================

    [Header("Scene Transition")]

    [SerializeField, Tooltip(
        "ResultSceneを表示してから" +
        "自動的にStartSceneへ戻るまでの秒数")]
    [Min(MinimumNonNegativeValue)]
    private float autoReturnTime =
        DefaultAutoReturnTime;


    [SerializeField, Tooltip(
        "Button4を押したときにStartSceneへ戻る")]
    private bool allowButton4Return =
        true;


    [SerializeField, Tooltip(
        "Enterキーを押したときにStartSceneへ戻る")]
    private bool allowEnterKeyReturn =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // 内部状態
    // ============================================================

    private float elapsedTime =
        MinimumNonNegativeValue;


    private int previousButton4 =
        0;


    private bool isChangingScene =
        false;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        // ========================================================
        // TimeScaleを通常に戻す
        // ========================================================

        Time.timeScale =
            1.0f;


        // ========================================================
        // 状態初期化
        // ========================================================

        elapsedTime =
            MinimumNonNegativeValue;


        isChangingScene =
            false;


        // ========================================================
        // スコア表示
        // ========================================================

        UpdateScoreText();


        // ========================================================
        // SensorRead取得
        // ========================================================

        ResolveSensorRead();


        // ========================================================
        // Button4初期状態
        // ========================================================
        //
        // 前シーンからButton4が押しっぱなしだった場合、
        // ResultSceneへ入った瞬間にStartSceneへ
        // 戻らないように現在値を保存する。
        // ========================================================

        if (sensorRead != null)
        {
            previousButton4 =
                sensorRead.GetButton4();
        }
        else
        {
            previousButton4 =
                0;
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


        // ========================================================
        // 経過時間
        // ========================================================

        elapsedTime +=
            Time.unscaledDeltaTime;


        // ========================================================
        // 一定時間経過
        // ========================================================

        if (
            elapsedTime >=
            autoReturnTime
        )
        {
            if (debugLog)
            {
                Debug.Log(
                    "ResultScene: " +
                    autoReturnTime +
                    "秒経過したためStartSceneへ戻ります。"
                );
            }


            ChangeToStartScene();


            return;
        }


        // ========================================================
        // Button4
        // ========================================================

        CheckButton4();


        if (isChangingScene)
        {
            return;
        }


        // ========================================================
        // Enterキー
        // ========================================================

        CheckEnterKey();
    }


    // ============================================================
    // Score表示
    // ============================================================

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            Debug.LogWarning(
                "ResultSceneManager: " +
                "Score Textが設定されていません。"
            );


            return;
        }


        int score =
            DataManager.GetScore();


        scoreText.text =
            score.ToString();


        if (debugLog)
        {
            Debug.Log(
                "ResultScene Score = " +
                score
            );
        }
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
            FindFirstObjectByType<
                SensorRead
            >();


        if (
            sensorRead == null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "ResultSceneManager: " +
                "SensorReadが見つかりません。" +
                "Enterキーまたは自動遷移は使用できます。"
            );
        }
    }


    // ============================================================
    // Button4チェック
    // ============================================================

    private void CheckButton4()
    {
        if (
            !allowButton4Return ||
            sensorRead == null ||
            isChangingScene
        )
        {
            return;
        }


        int currentButton4 =
            sensorRead.GetButton4();


        // ========================================================
        // 0 → 1 の瞬間だけ反応
        // ========================================================

        if (
            currentButton4 == 1 &&
            previousButton4 != 1
        )
        {
            if (debugLog)
            {
                Debug.Log(
                    "ResultScene: " +
                    "Button4が押されたためStartSceneへ戻ります。"
                );
            }


            ChangeToStartScene();


            return;
        }


        previousButton4 =
            currentButton4;
    }


    // ============================================================
    // Enterキーチェック
    // ============================================================

    private void CheckEnterKey()
    {
        if (
            !allowEnterKeyReturn ||
            isChangingScene
        )
        {
            return;
        }


        if (Keyboard.current == null)
        {
            return;
        }


        bool enterPressed =
            Keyboard.current
                .enterKey
                .wasPressedThisFrame;


        // テンキーのEnterにも対応
        bool numpadEnterPressed =
            Keyboard.current
                .numpadEnterKey
                .wasPressedThisFrame;


        if (
            !enterPressed &&
            !numpadEnterPressed
        )
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "ResultScene: " +
                "Enterキーが押されたためStartSceneへ戻ります。"
            );
        }


        ChangeToStartScene();
    }


    // ============================================================
    // StartSceneへ移動
    // ============================================================

    private void ChangeToStartScene()
    {
        if (isChangingScene)
        {
            return;
        }


        isChangingScene =
            true;


        // 念のためTimeScaleを通常状態にする
        Time.timeScale =
            1.0f;


        if (debugLog)
        {
            Debug.Log(
                "ResultScene → StartScene"
            );
        }


        SceneManager.LoadScene(
            StartSceneName
        );
    }


    // ============================================================
    // 外部からStartSceneへ戻す
    // ============================================================

    public void ReturnToStartScene()
    {
        ChangeToStartScene();
    }


    // ============================================================
    // Getter
    // ============================================================

    public float GetElapsedTime()
    {
        return
            elapsedTime;
    }


    public float GetRemainingTime()
    {
        return
            Mathf.Max(
                MinimumNonNegativeValue,
                autoReturnTime -
                elapsedTime
            );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        autoReturnTime =
            Mathf.Max(
                MinimumNonNegativeValue,
                autoReturnTime
            );
    }
}