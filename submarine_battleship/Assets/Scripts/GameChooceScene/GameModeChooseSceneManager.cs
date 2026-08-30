using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameModeChooseSceneManager : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const int ButtonReleased =
        0;

    private const int ButtonPressed =
        1;

    private const float DefaultReleaseConfirmationDuration =
        0.20f;

    private const float MinimumNonNegativeValue =
        0.0f;


    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "Button1～6を取得するSensorRead。" +
        "未設定の場合は自動検索する")]
    private SensorRead sensorRead;


    // ============================================================
    // Scene
    // ============================================================

    [Header("Scene")]

    [SerializeField, Tooltip(
        "赤いボタン（Button2）を押したときの遷移先。" +
        "従来の光信号ゲームのシーン名")]
    private string redButtonSceneName =
        "MainScene";


    [SerializeField, Tooltip(
        "青いボタン（Button3）を押したときの遷移先。" +
        "色の順番を覚えるゲームのシーン名")]
    private string blueButtonSceneName =
        "MainScene_ColorMemory";


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "ゲーム選択内容を表示するTextMeshPro。" +
        "未設定でも動作可能")]
    private TMP_Text selectionText;


    [SerializeField, Tooltip(
        "現在入力を受け付けられるかを表示するTextMeshPro。" +
        "未設定でも動作可能")]
    private TMP_Text statusText;


    [SerializeField, Tooltip(
        "赤いボタン側のゲーム名")]
    private string redGameLabel =
        "光信号ゲーム";


    [SerializeField, Tooltip(
        "青いボタン側のゲーム名")]
    private string blueGameLabel =
        "色記憶ゲーム";


    // ============================================================
    // Input Guard
    // ============================================================

    [Header("Input Guard")]

    [SerializeField, Min(MinimumNonNegativeValue), Tooltip(
        "シーン開始後、Button1～6がすべて離された状態が" +
        "この時間連続したら選択入力を受け付ける。" +
        "前シーンからの押しっぱなしや、" +
        "シリアル通信開始直後の誤判定を防ぐ")]
    private float releaseConfirmationDuration =
        DefaultReleaseConfirmationDuration;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        true;


    // ============================================================
    // Internal
    // ============================================================

    private bool inputArmed =
        false;


    private bool isChangingScene =
        false;


    private float allButtonsReleasedTime =
        MinimumNonNegativeValue;


    private int previousButton2 =
        ButtonReleased;


    private int previousButton3 =
        ButtonReleased;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        // 前シーンでTimeScaleを変更していた場合に備える
        Time.timeScale =
            1.0f;
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        SetupUI();


        // ========================================================
        // Start時点では絶対に入力受付を開始しない
        //
        // SensorReadのシリアル通信開始直後は、
        // 最初の受信値が届くまでButton値が0の可能性がある。
        //
        // Update内で
        //
        // Button1～6がすべて0
        //      ↓
        // 一定時間その状態を維持
        //      ↓
        // 入力受付開始
        //
        // とする。
        // ========================================================

        inputArmed =
            false;


        allButtonsReleasedTime =
            MinimumNonNegativeValue;


        previousButton2 =
            ButtonReleased;


        previousButton3 =
            ButtonReleased;


        UpdateStatusText();


        if (sensorRead == null)
        {
            Debug.LogError(
                "GameModeChooseSceneManager: " +
                "SensorReadが見つかりません。"
            );
        }


        if (debugLog)
        {
            Debug.Log(
                "Game Mode Choose Scene Ready. " +
                "全Buttonの解放待ちです。"
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


        // ========================================================
        // SensorRead再検索
        // ========================================================

        if (sensorRead == null)
        {
            ResolveReferences();


            if (sensorRead == null)
            {
                return;
            }
        }


        // ========================================================
        // 現在の全Button値
        // ========================================================

        ReadCurrentButtons(
            out int button1,
            out int button2,
            out int button3,
            out int button4,
            out int button5,
            out int button6
        );


        // ========================================================
        // まだ入力受付開始前
        // ========================================================

        if (!inputArmed)
        {
            UpdateInputArming(
                button1,
                button2,
                button3,
                button4,
                button5,
                button6
            );


            previousButton2 =
                button2;


            previousButton3 =
                button3;


            return;
        }


        // ========================================================
        // Button2
        // 0 → 1
        // ========================================================

        bool redButtonPressed =
            button2 ==
                ButtonPressed
            &&
            previousButton2 !=
                ButtonPressed;


        // ========================================================
        // Button3
        // 0 → 1
        // ========================================================

        bool blueButtonPressed =
            button3 ==
                ButtonPressed
            &&
            previousButton3 !=
                ButtonPressed;


        // ========================================================
        // Button2とButton3の同時押し
        //
        // 意図しないゲーム選択を避けるため、
        // 同時押しの場合はどちらにも進まない。
        // ========================================================

        if (
            redButtonPressed &&
            blueButtonPressed
        )
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "Button2とButton3が同時に押されました。" +
                    "ゲーム選択は行いません。" +
                    "一度離してから片方だけ押してください。"
                );
            }
        }

        // ========================================================
        // 赤いボタン
        // ========================================================

        else if (redButtonPressed)
        {
            SelectRedGame();
        }

        // ========================================================
        // 青いボタン
        // ========================================================

        else if (blueButtonPressed)
        {
            SelectBlueGame();
        }


        // ========================================================
        // 前回値保存
        // ========================================================

        previousButton2 =
            button2;


        previousButton3 =
            button3;
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (sensorRead != null)
        {
            return;
        }


        sensorRead =
            FindFirstObjectByType<
                SensorRead
            >();
    }


    // ============================================================
    // 全Button取得
    // ============================================================

    private void ReadCurrentButtons(
        out int button1,
        out int button2,
        out int button3,
        out int button4,
        out int button5,
        out int button6
    )
    {
        button1 =
            sensorRead.GetButton1();


        button2 =
            sensorRead.GetButton2();


        button3 =
            sensorRead.GetButton3();


        button4 =
            sensorRead.GetButton4();


        button5 =
            sensorRead.GetButton5();


        button6 =
            sensorRead.GetButton6();
    }


    // ============================================================
    // 入力受付開始判定
    // ============================================================

    private void UpdateInputArming(
        int button1,
        int button2,
        int button3,
        int button4,
        int button5,
        int button6
    )
    {
        // ========================================================
        // Button1～6が全部0か
        // ========================================================

        bool allButtonsReleased =
            button1 ==
                ButtonReleased
            &&
            button2 ==
                ButtonReleased
            &&
            button3 ==
                ButtonReleased
            &&
            button4 ==
                ButtonReleased
            &&
            button5 ==
                ButtonReleased
            &&
            button6 ==
                ButtonReleased;


        // ========================================================
        // 1つでも押されている
        //
        // 解放確認時間をリセット
        // ========================================================

        if (!allButtonsReleased)
        {
            allButtonsReleasedTime =
                MinimumNonNegativeValue;


            UpdateStatusText();


            return;
        }


        // ========================================================
        // 全Buttonが0の時間を計測
        //
        // TimeScaleの影響を受けないよう
        // unscaledDeltaTimeを使用
        // ========================================================

        allButtonsReleasedTime +=
            Time.unscaledDeltaTime;


        // ========================================================
        // まだ確認時間不足
        // ========================================================

        if (
            allButtonsReleasedTime <
            releaseConfirmationDuration
        )
        {
            UpdateStatusText();


            return;
        }


        // ========================================================
        // 入力受付開始
        // ========================================================

        inputArmed =
            true;


        allButtonsReleasedTime =
            releaseConfirmationDuration;


        // この瞬間はButton2/3とも0なので
        // 前回値も0として開始する
        previousButton2 =
            ButtonReleased;


        previousButton3 =
            ButtonReleased;


        UpdateStatusText();


        if (debugLog)
        {
            Debug.Log(
                "すべてのButtonの解放を確認しました。" +
                "ゲーム選択入力を受け付けます。"
            );
        }
    }


    // ============================================================
    // 赤いボタン
    // ============================================================

    private void SelectRedGame()
    {
        if (isChangingScene)
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "Game Mode Select: RED / " +
                redGameLabel +
                " → " +
                redButtonSceneName
            );
        }


        ChangeScene(
            redButtonSceneName
        );
    }


    // ============================================================
    // 青いボタン
    // ============================================================

    private void SelectBlueGame()
    {
        if (isChangingScene)
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "Game Mode Select: BLUE / " +
                blueGameLabel +
                " → " +
                blueButtonSceneName
            );
        }


        ChangeScene(
            blueButtonSceneName
        );
    }


    // ============================================================
    // Scene Change
    // ============================================================

    private void ChangeScene(
        string sceneName
    )
    {
        if (isChangingScene)
        {
            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                sceneName
            )
        )
        {
            Debug.LogError(
                "GameModeChooseSceneManager: " +
                "遷移先Scene名が空です。"
            );


            return;
        }


        isChangingScene =
            true;


        Time.timeScale =
            1.0f;


        SceneManager.LoadScene(
            sceneName
        );
    }


    // ============================================================
    // UI
    // ============================================================

    private void SetupUI()
    {
        if (selectionText == null)
        {
            return;
        }


        selectionText.text =
            "プレイするゲームを選択してください\n\n" +

            "<b>赤いボタン</b>\n" +
            redGameLabel +
            "\n\n" +

            "<b>青いボタン</b>\n" +
            blueGameLabel;
    }


    private void UpdateStatusText()
    {
        if (statusText == null)
        {
            return;
        }


        if (inputArmed)
        {
            statusText.text =
                "ゲームを選択できます";
        }
        else
        {
            statusText.text =
                "すべてのボタンを離してください";
        }
    }


    // ============================================================
    // Getter
    // ============================================================

    public bool GetInputArmed()
    {
        return
            inputArmed;
    }


    public bool GetIsChangingScene()
    {
        return
            isChangingScene;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        releaseConfirmationDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                releaseConfirmationDuration
            );


        if (
            string.IsNullOrWhiteSpace(
                redButtonSceneName
            )
        )
        {
            redButtonSceneName =
                "MainScene";
        }


        if (
            string.IsNullOrWhiteSpace(
                blueButtonSceneName
            )
        )
        {
            blueButtonSceneName =
                "MainScene_ColorMemory";
        }
    }
}