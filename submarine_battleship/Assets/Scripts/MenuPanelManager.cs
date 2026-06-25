using UnityEngine;
using UnityEngine.InputSystem;

public class MenuPanelManager : MonoBehaviour
{
    [SerializeField, Tooltip("管理者操作用のパネル")]
    private GameObject administratorMenuPanel;

    [SerializeField, Tooltip("ソナー表示用のパネル")]
    private GameObject sonarPanel;

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
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleAdministratorMenu();
        }

        UpdateSonarPanelBySpaceKey();
    }

    // Getter
    public static float GetGameTimeScale()
    {
        return gameTimeScale;
    }

    // Setter
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

    private void ToggleAdministratorMenu()
    {
        isAdministratorMenuOpen = !isAdministratorMenuOpen;

        SetAdministratorMenu(isAdministratorMenuOpen);

        if (isAdministratorMenuOpen)
        {
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

    private void UpdateSonarPanelBySpaceKey()
    {
        if (sonarPanel == null) return;

        // 管理者メニューが開いている間はソナーを出さない
        if (isAdministratorMenuOpen)
        {
            SetSonarPanel(false);
            return;
        }

        bool shouldShowSonar = Keyboard.current.spaceKey.isPressed;
        SetSonarPanel(shouldShowSonar);
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