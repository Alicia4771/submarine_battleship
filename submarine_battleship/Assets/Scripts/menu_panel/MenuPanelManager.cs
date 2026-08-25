using UnityEngine;
using UnityEngine.InputSystem;

public class MenuPanelManager : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultGameTimeScale =
        1.0f;

    private const float MinimumGameTimeScale =
        0.0f;

    private const float MaximumGameTimeScale =
        100.0f;


    // ============================================================
    // パネル
    // ============================================================

    [Header("Panels")]

    [SerializeField, Tooltip(
        "管理者操作用のパネル")]
    private GameObject
        administratorMenuPanel;


    [SerializeField, Tooltip(
        "ソナー表示用のパネル")]
    private GameObject
        sonarPanel;


    // ============================================================
    // ソナー
    // ============================================================

    [Header("Sonar")]

    [SerializeField, Tooltip(
        "ONの場合、潜望鏡の高さに関係なくソナーを使用可能。" +
        "通常ゲームではOFF推奨")]
    private bool sonarPanelUnderwaterCanOpen =
        false;


    [SerializeField, Tooltip(
        "Spaceキーをソナーのテスト入力として使用する")]
    private bool allowKeyboardSonarInput =
        true;


    // ============================================================
    // 状態
    // ============================================================

    private bool isAdministratorMenuOpen =
        false;

    private bool isSonarPanelOpen =
        false;


    // ============================================================
    // static
    // ============================================================

    private static float gameTimeScale =
        DefaultGameTimeScale;


    private static bool
        isAdministratorMenuOpenStatic =
            false;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        SetAdministratorMenu(
            false
        );


        SetSonarPanel(
            false
        );


        Time.timeScale =
            gameTimeScale;
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (
            Keyboard.current != null &&
            Keyboard.current
                .escapeKey
                .wasPressedThisFrame
        )
        {
            ToggleAdministratorMenu();
        }


        UpdateSonarPanel();
    }


    // ============================================================
    // TimeScale
    // ============================================================

    public static float GetGameTimeScale()
    {
        return
            gameTimeScale;
    }


    public static bool SetGameTimeScale(
        float value
    )
    {
        if (
            value <
            MinimumGameTimeScale
        )
        {
            Debug.LogError(
                "TimeScaleには0以上の値を設定してください: " +
                value
            );

            return false;
        }


        gameTimeScale =
            Mathf.Clamp(
                value,
                MinimumGameTimeScale,
                MaximumGameTimeScale
            );


        if (!isAdministratorMenuOpenStatic)
        {
            Time.timeScale =
                gameTimeScale;
        }


        return true;
    }


    // ============================================================
    // 管理者メニュー状態
    // ============================================================

    public static bool
        GetIsAdministratorMenuOpen()
    {
        return
            isAdministratorMenuOpenStatic;
    }


    // ============================================================
    // Sonar設定互換API
    // ============================================================

    public bool
        GetSonarPanelUnderwaterCanOpen()
    {
        return
            sonarPanelUnderwaterCanOpen;
    }


    public void
        SetSonarPanelUnderwaterCanOpen(
            bool canOpen
        )
    {
        sonarPanelUnderwaterCanOpen =
            canOpen;
    }


    // ============================================================
    // 管理者メニュー
    // ============================================================

    private void ToggleAdministratorMenu()
    {
        bool newState =
            !isAdministratorMenuOpen;


        SetAdministratorMenu(
            newState
        );


        if (newState)
        {
            SetSonarPanel(
                false
            );


            Time.timeScale =
                MinimumGameTimeScale;
        }
        else
        {
            Time.timeScale =
                gameTimeScale;
        }
    }


    // ============================================================
    // Sonar
    // ============================================================

    private void UpdateSonarPanel()
    {
        if (sonarPanel == null)
        {
            return;
        }


        // 管理者メニュー中は使用不可
        if (isAdministratorMenuOpen)
        {
            SetSonarPanel(
                false
            );

            return;
        }


        // 潜望鏡状態
        if (!CanOpenSonar())
        {
            SetSonarPanel(
                false
            );

            return;
        }


        bool sonarButtonPressed =
            IsSonarButtonPressed();


        bool keyboardPressed =
            allowKeyboardSonarInput &&
            IsSpacePressed();


        bool shouldOpen =
            sonarButtonPressed ||
            keyboardPressed;


        SetSonarPanel(
            shouldOpen
        );
    }


    // ============================================================
    // Sonar使用可能か
    // ============================================================

    private bool CanOpenSonar()
    {
        // 管理者設定などで
        // 高さ制限を無効化
        if (sonarPanelUnderwaterCanOpen)
        {
            return true;
        }


        // 通常ゲームでは
        // 潜望鏡が完全格納されている時だけ
        return
            DataManager
                .GetIsPeriscopeFullyLowered();
    }


    // ============================================================
    // Input
    // ============================================================

    private bool IsSpacePressed()
    {
        return
            Keyboard.current != null &&
            Keyboard.current
                .spaceKey
                .isPressed;
    }


    private bool IsSonarButtonPressed()
    {
        return
            DataManager
                .GetSensorButton1()
            ==
            1;
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


        if (
            administratorMenuPanel !=
            null
        )
        {
            administratorMenuPanel
                .SetActive(
                    isOpen
                );
        }
    }


    // ============================================================
    // Sonar Panel
    // ============================================================

    private void SetSonarPanel(
        bool isOpen
    )
    {
        isSonarPanelOpen =
            isOpen;


        if (
            sonarPanel != null &&
            sonarPanel.activeSelf !=
            isOpen
        )
        {
            sonarPanel.SetActive(
                isOpen
            );
        }
    }


    // ============================================================
    // 状態取得
    // ============================================================

    public bool GetIsSonarPanelOpen()
    {
        return
            isSonarPanelOpen;
    }


    public void CloseSonarPanel()
    {
        SetSonarPanel(
            false
        );
    }
}