using UnityEngine;
using UnityEngine.InputSystem;

public class MenuPanelManager : MonoBehaviour
{
    // =========================
    // パネル
    // =========================

    [SerializeField, Tooltip("管理者操作用のパネル")]
    private GameObject administratorMenuPanel;


    [SerializeField, Tooltip("ソナー表示用のパネル")]
    private GameObject sonarPanel;


    // =========================
    // ソナー設定
    // =========================

    [SerializeField, Tooltip(
        "falseの場合、潜水艦が水中の時だけソナーを開ける")]
    private bool sonarPanelUnderwaterCanOpen = false;


    // =========================
    // パネル状態
    // =========================

    private bool isAdministratorMenuOpen = false;
    private bool isSonarPanelOpen = false;


    // =========================
    // static状態
    // =========================

    private static float gameTimeScale = 1f;

    private static bool isAdministratorMenuOpenStatic = false;


    // =========================
    // Start
    // =========================

    void Start()
    {
        SetAdministratorMenu(false);

        SetSonarPanel(false);


        Time.timeScale =
            gameTimeScale;
    }


    // =========================
    // Update
    // =========================

    void Update()
    {
        // Escapeキーで管理者メニューを開閉
        if (
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame
        )
        {
            ToggleAdministratorMenu();
        }


        // ソナーパネル更新
        UpdateSonarPanel();
    }


    // ============================================================
    // Game Time Scale
    // ============================================================

    public static float GetGameTimeScale()
    {
        return gameTimeScale;
    }


    public static bool SetGameTimeScale(
        float value
    )
    {
        if (value < 0f)
        {
            Debug.LogError(
                "TimeScaleには0以上の値を設定してください: "
                + value
            );

            return false;
        }


        gameTimeScale =
            Mathf.Clamp(
                value,
                0f,
                100f
            );


        // 管理者メニューが開いていないときだけ
        // 実際に反映
        if (!isAdministratorMenuOpenStatic)
        {
            Time.timeScale =
                gameTimeScale;
        }


        return true;
    }


    // ============================================================
    // 管理者メニュー
    // ============================================================

    public static bool GetIsAdministratorMenuOpen()
    {
        return isAdministratorMenuOpenStatic;
    }


    // ============================================================
    // ソナー設定
    // ============================================================

    public bool GetSonarPanelUnderwaterCanOpen()
    {
        return sonarPanelUnderwaterCanOpen;
    }


    public void SetSonarPanelUnderwaterCanOpen(
        bool canOpen
    )
    {
        sonarPanelUnderwaterCanOpen =
            canOpen;
    }


    // ============================================================
    // 管理者メニュー開閉
    // ============================================================

    private void ToggleAdministratorMenu()
    {
        isAdministratorMenuOpen =
            !isAdministratorMenuOpen;


        SetAdministratorMenu(
            isAdministratorMenuOpen
        );


        if (isAdministratorMenuOpen)
        {
            // 管理者メニューを開いたら、
            // ソナーは強制的に閉じる
            SetSonarPanel(false);


            // 管理者メニュー中は一時停止
            Time.timeScale = 0f;
        }
        else
        {
            // 閉じたら設定済みの
            // TimeScaleに戻す
            Time.timeScale =
                gameTimeScale;
        }
    }


    // ============================================================
    // ソナーパネル更新
    // ============================================================

    private void UpdateSonarPanel()
    {
        if (sonarPanel == null)
        {
            return;
        }


        // 管理者メニューが開いている間は、
        // SpaceやButton1を押しても
        // ソナーを表示しない
        if (isAdministratorMenuOpen)
        {
            SetSonarPanel(false);

            return;
        }


        // =========================
        // 入力取得
        // =========================

        bool isSpacePressed =
            IsSpacePressed();


        bool isSonarButtonPressed =
            IsSonarButtonPressed();


        bool isInputPressed =
            isSpacePressed ||
            isSonarButtonPressed;


        // =========================
        // ソナーを開ける深度か確認
        // =========================

        if (!CanOpenSonarBySubmarinePosition())
        {
            SetSonarPanel(false);

            return;
        }


        // Spaceキーを押している間、
        // またはButton1を押している間だけ表示
        SetSonarPanel(
            isInputPressed
        );
    }


    // ============================================================
    // Spaceキー
    // ============================================================

    private bool IsSpacePressed()
    {
        return
            Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed;
    }


    // ============================================================
    // ソナーボタン
    // ============================================================

    private bool IsSonarButtonPressed()
    {
        // Button1をソナー用ボタンとして使用
        return
            DataManager.GetSensorButton1() == 1;
    }


    // ============================================================
    // ソナーを開ける深度か
    // ============================================================

    private bool CanOpenSonarBySubmarinePosition()
    {
        // trueなら、
        // 潜水艦が水上でも水中でも
        // ソナーを開ける
        if (sonarPanelUnderwaterCanOpen)
        {
            return true;
        }


        // falseなら、
        // 潜水艦が水中、
        // つまりY座標が0未満の時だけ開ける
        Vector3 submarinePosition =
            DataManager.GetSubmarinePosition();


        return
            submarinePosition.y < 0f;
    }


    // ============================================================
    // 管理者メニュー設定
    // ============================================================

    private void SetAdministratorMenu(
        bool isOpen
    )
    {
        isAdministratorMenuOpen =
            isOpen;


        isAdministratorMenuOpenStatic =
            isOpen;


        if (administratorMenuPanel != null)
        {
            administratorMenuPanel.SetActive(
                isOpen
            );
        }
    }


    // ============================================================
    // ソナーパネル設定
    // ============================================================

    private void SetSonarPanel(
        bool isOpen
    )
    {
        isSonarPanelOpen =
            isOpen;


        if (sonarPanel != null)
        {
            sonarPanel.SetActive(
                isOpen
            );
        }
    }
}