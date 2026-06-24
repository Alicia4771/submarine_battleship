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

    void Start()
    {
        SetAdministratorMenu(false);
        SetSonarPanel(false);

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Escキーで管理者メニューの開閉
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleAdministratorMenu();
        }

        // Spaceキーを押している間だけソナー表示
        UpdateSonarPanelBySpaceKey();
    }

    private void ToggleAdministratorMenu()
    {
        isAdministratorMenuOpen = !isAdministratorMenuOpen;

        SetAdministratorMenu(isAdministratorMenuOpen);

        if (isAdministratorMenuOpen)
        {
            // 管理者画面を開いたら、ソナー画面は強制的に閉じる
            SetSonarPanel(false);
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void UpdateSonarPanelBySpaceKey()
    {
        if (sonarPanel == null) return;

        // 管理者メニューが開いている間は、Spaceを押していてもソナーを出さない
        if (isAdministratorMenuOpen)
        {
            SetSonarPanel(false);
            return;
        }

        // Spaceキーを押している間だけtrue
        bool shouldShowSonar = Keyboard.current.spaceKey.isPressed;

        SetSonarPanel(shouldShowSonar);
    }

    private void SetAdministratorMenu(bool isOpen)
    {
        isAdministratorMenuOpen = isOpen;

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