using UnityEngine;
using UnityEngine.InputSystem;

public class MenuPanelManager : MonoBehaviour
{
    [SerializeField, Tooltip("管理者操作用のパネル")]
    private GameObject administratorMenuPanel;

    [SerializeField, Tooltip("ソナー表示用のパネル")]
    private GameObject sonarPanel;

    [SerializeField, Tooltip("センサー読み取り用スクリプト")]
    private SensorRead sensorRead;

    [SerializeField, Tooltip("falseの場合、潜水艦が水中の時だけソナーを開ける")]
    private bool sonarPanelUnderwaterCanOpen = false;

    private bool isAdministratorMenuOpen = false;
    private bool isSonarPanelOpen = false;

    private static float gameTimeScale = 1f;
    private static bool isAdministratorMenuOpenStatic = false;

    void Start()
    {
        SetAdministratorMenu(false);
        SetSonarPanel(false);

        Time.timeScale = gameTimeScale;
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleAdministratorMenu();
        }

        UpdateSonarPanel();
    }

    public static float GetGameTimeScale()
    {
        return gameTimeScale;
    }

    public static bool SetGameTimeScale(float value)
    {
        if (value < 0f)
        {
            Debug.LogError("TimeScaleには0以上の値を設定してください: " + value);
            return false;
        }

        gameTimeScale = Mathf.Clamp(value, 0f, 100f);

        // 管理者メニューが開いていないときだけ実際に反映
        if (!isAdministratorMenuOpenStatic)
        {
            Time.timeScale = gameTimeScale;
        }

        return true;
    }

    public static bool GetIsAdministratorMenuOpen()
    {
        return isAdministratorMenuOpenStatic;
    }

    public bool GetSonarPanelUnderwaterCanOpen()
    {
        return sonarPanelUnderwaterCanOpen;
    }

    public void SetSonarPanelUnderwaterCanOpen(bool canOpen)
    {
        sonarPanelUnderwaterCanOpen = canOpen;
    }

    private void ToggleAdministratorMenu()
    {
        isAdministratorMenuOpen = !isAdministratorMenuOpen;

        SetAdministratorMenu(isAdministratorMenuOpen);

        if (isAdministratorMenuOpen)
        {
            // 管理者メニューを開いたら、ソナーは強制的に閉じる
            SetSonarPanel(false);

            // 管理者メニュー中は一時停止
            Time.timeScale = 0f;
        }
        else
        {
            // 閉じたら設定済みのTimeScaleに戻す
            Time.timeScale = gameTimeScale;
        }
    }

    private void UpdateSonarPanel()
    {
        if (sonarPanel == null) return;

        // 管理者メニューが開いている間は、Spaceやタクトスイッチを押してもソナーを出さない
        if (isAdministratorMenuOpen)
        {
            SetSonarPanel(false);
            return;
        }

        bool isSpacePressed = IsSpacePressed();
        bool isTactileSwitchPressed = IsTactileSwitchPressed();
        bool isInputPressed = isSpacePressed || isTactileSwitchPressed;

        // sonarPanelUnderwaterCanOpen が false の場合は、水中でしか開けない
        if (!CanOpenSonarBySubmarinePosition())
        {
            SetSonarPanel(false);
            return;
        }

        // Spaceキーを押している間、またはタクトスイッチが1の間だけ表示
        SetSonarPanel(isInputPressed);
    }

    private bool IsSpacePressed()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    }

    private bool IsTactileSwitchPressed()
    {
        if (sensorRead == null) return false;

        return sensorRead.GetTactileSwitch() == 1;
    }

    private bool CanOpenSonarBySubmarinePosition()
    {
        // trueなら、潜水艦が水上でも水中でもソナーを開ける
        if (sonarPanelUnderwaterCanOpen)
        {
            return true;
        }

        // falseなら、潜水艦が水中、つまりY座標が0未満の時だけ開ける
        Vector3 submarinePosition = DataManager.GetSubmarinePosition();

        return submarinePosition.y < 0f;
    }

    private void SetAdministratorMenu(bool isOpen)
    {
        isAdministratorMenuOpen = isOpen;
        isAdministratorMenuOpenStatic = isOpen;

        if (administratorMenuPanel != null)
        {
            administratorMenuPanel.SetActive(isOpen);
        }
    }

    private void SetSonarPanel(bool isOpen)
    {
        isSonarPanelOpen = isOpen;

        if (sonarPanel != null)
        {
            sonarPanel.SetActive(isOpen);
        }
    }
}