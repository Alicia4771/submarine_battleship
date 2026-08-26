using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class TutoChooseSceneManager : MonoBehaviour
{
    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "ボタン入力を取得するSensorRead")]
    private SensorRead sensorRead;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "選択方法を表示するTextMeshPro。" +
        "未設定でも動作可能")]
    private TMP_Text selectText;


    // ============================================================
    // Scene
    // ============================================================

    [Header("Scene")]

    [SerializeField, Tooltip(
        "チュートリアルをスキップした場合の遷移先")]
    private string mainSceneName =
        "MainScene";


    [SerializeField, Tooltip(
        "通常版チュートリアルのシーン名")]
    private string longTutorialSceneName =
        "Tuto_long";


    [SerializeField, Tooltip(
        "簡略版チュートリアルのシーン名")]
    private string shortTutorialSceneName =
        "Tuto_short";


    // ============================================================
    // Keyboard Test
    // ============================================================

    [Header("Keyboard Test")]

    [SerializeField, Tooltip(
        "Unity Editorで数字キーによるテストを許可する")]
    private bool allowKeyboardTest =
        true;


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

    private int previousButton1 =
        0;

    private int previousButton2 =
        0;

    private int previousButton3 =
        0;


    // ============================================================
    // 入力受付開始フラグ
    //
    // StartSceneで黒いボタンを押したまま
    // Tuto_chooseへ来た場合、その入力で即スキップされるのを防ぐ。
    //
    // 3つのボタンを一度すべて離すまでfalse。
    // ============================================================

    private bool inputArmed =
        false;


    private bool isChangingScene =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
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


        if (sensorRead != null)
        {
            previousButton1 =
                sensorRead.GetButton1();

            previousButton2 =
                sensorRead.GetButton2();

            previousButton3 =
                sensorRead.GetButton3();


            // ====================================================
            // 最初から全部離れていれば即受付可能
            // ====================================================

            inputArmed =
                previousButton1 == 0 &&
                previousButton2 == 0 &&
                previousButton3 == 0;
        }
        else
        {
            Debug.LogWarning(
                "TutoChooseSceneManager: " +
                "SensorReadが見つかりません。"
            );


            // SensorReadがなくても
            // キーボードテストは可能
            inputArmed =
                true;
        }


        if (debugLog)
        {
            Debug.Log(
                "Tutorial Choose Scene Ready."
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


        CheckPhysicalButtons();


        CheckKeyboard();
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (sensorRead == null)
        {
            sensorRead =
                FindFirstObjectByType<
                    SensorRead
                >();
        }
    }


    // ============================================================
    // UI
    // ============================================================

    private void SetupUI()
    {
        if (selectText == null)
        {
            return;
        }


        selectText.text =
            "チュートリアルを選択してください\n\n" +

            "<b>黒いボタン</b>\n" +
            "チュートリアルをスキップしてゲーム開始\n\n" +

            "<b>赤いボタン</b>\n" +
            "通常チュートリアル\n" +
            "ゲームの流れを順番に体験します\n\n" +

            "<b>青いボタン</b>\n" +
            "簡略チュートリアル\n" +
            "必要な操作だけを短時間で確認します";
    }


    // ============================================================
    // Physical Buttons
    // ============================================================

    private void CheckPhysicalButtons()
    {
        if (sensorRead == null)
        {
            return;
        }


        int currentButton1 =
            sensorRead.GetButton1();

        int currentButton2 =
            sensorRead.GetButton2();

        int currentButton3 =
            sensorRead.GetButton3();


        // ========================================================
        // 前シーンから押しっぱなしの入力を無視
        //
        // 全ボタンを一度離してから選択可能にする。
        // ========================================================

        if (!inputArmed)
        {
            if (
                currentButton1 == 0 &&
                currentButton2 == 0 &&
                currentButton3 == 0
            )
            {
                inputArmed =
                    true;


                if (debugLog)
                {
                    Debug.Log(
                        "Tutorial selection input armed."
                    );
                }
            }


            previousButton1 =
                currentButton1;

            previousButton2 =
                currentButton2;

            previousButton3 =
                currentButton3;


            return;
        }


        // ========================================================
        // Button1
        // 黒いボタン
        // → Skip
        // ========================================================

        bool button1Pressed =
            currentButton1 == 1 &&
            previousButton1 != 1;


        // ========================================================
        // Button2
        // 赤いボタン
        // → Long Tutorial
        // ========================================================

        bool button2Pressed =
            currentButton2 == 1 &&
            previousButton2 != 1;


        // ========================================================
        // Button3
        // 青いボタン
        // → Short Tutorial
        // ========================================================

        bool button3Pressed =
            currentButton3 == 1 &&
            previousButton3 != 1;


        // ========================================================
        // 優先順位
        //
        // 同時押しされた場合、
        // Button1 → Button2 → Button3の順で判定。
        // ========================================================

        if (button1Pressed)
        {
            SelectSkip();
        }
        else if (button2Pressed)
        {
            SelectLongTutorial();
        }
        else if (button3Pressed)
        {
            SelectShortTutorial();
        }


        previousButton1 =
            currentButton1;

        previousButton2 =
            currentButton2;

        previousButton3 =
            currentButton3;
    }


    // ============================================================
    // Keyboard Test
    // ============================================================

    private void CheckKeyboard()
    {
        if (
            !allowKeyboardTest ||
            Keyboard.current == null ||
            isChangingScene
        )
        {
            return;
        }


        // ========================================================
        // 1
        // → 黒いボタン
        // → Skip
        // ========================================================

        if (
            Keyboard.current
                .digit1Key
                .wasPressedThisFrame
            ||
            Keyboard.current
                .numpad1Key
                .wasPressedThisFrame
        )
        {
            SelectSkip();

            return;
        }


        // ========================================================
        // 2
        // → 赤いボタン
        // → Long
        // ========================================================

        if (
            Keyboard.current
                .digit2Key
                .wasPressedThisFrame
            ||
            Keyboard.current
                .numpad2Key
                .wasPressedThisFrame
        )
        {
            SelectLongTutorial();

            return;
        }


        // ========================================================
        // 3
        // → 青いボタン
        // → Short
        // ========================================================

        if (
            Keyboard.current
                .digit3Key
                .wasPressedThisFrame
            ||
            Keyboard.current
                .numpad3Key
                .wasPressedThisFrame
        )
        {
            SelectShortTutorial();
        }
    }


    // ============================================================
    // Skip
    // ============================================================

    private void SelectSkip()
    {
        if (isChangingScene)
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "Tutorial selection: SKIP"
            );
        }


        ChangeScene(
            mainSceneName
        );
    }


    // ============================================================
    // Long Tutorial
    // ============================================================

    private void SelectLongTutorial()
    {
        if (isChangingScene)
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "Tutorial selection: LONG"
            );
        }


        ChangeScene(
            longTutorialSceneName
        );
    }


    // ============================================================
    // Short Tutorial
    // ============================================================

    private void SelectShortTutorial()
    {
        if (isChangingScene)
        {
            return;
        }


        if (debugLog)
        {
            Debug.Log(
                "Tutorial selection: SHORT"
            );
        }


        ChangeScene(
            shortTutorialSceneName
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
                "TutoChooseSceneManager: " +
                "遷移先シーン名が空です。"
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
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        if (
            string.IsNullOrWhiteSpace(
                mainSceneName
            )
        )
        {
            mainSceneName =
                "MainScene";
        }


        if (
            string.IsNullOrWhiteSpace(
                longTutorialSceneName
            )
        )
        {
            longTutorialSceneName =
                "Tuto_long";
        }


        if (
            string.IsNullOrWhiteSpace(
                shortTutorialSceneName
            )
        )
        {
            shortTutorialSceneName =
                "Tuto_short";
        }
    }
}