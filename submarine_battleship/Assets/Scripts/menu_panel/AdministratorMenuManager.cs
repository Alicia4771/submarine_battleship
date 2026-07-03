using UnityEngine;

public class AdministratorMenuManager : MonoBehaviour
{
    [SerializeField, Tooltip("管理者メニューの最初の一覧画面")]
    private GameObject mainPanel;

    [SerializeField, Tooltip("シーン設定の詳細画面")]
    private GameObject sceneSettingPanel;

    [SerializeField, Tooltip("ゲーム設定の詳細画面")]
    private GameObject gameSettingPanel;

    [SerializeField, Tooltip("デバッグ情報の詳細画面")]
    private GameObject debugInfoDetailPanel;
    


    void OnEnable()
    {
        // 管理者メニューを開いたときは、必ず一覧画面から表示する
        ShowMainPanel();
    }

    public void ShowMainPanel()
    {
        SetAllPanelsInactive();
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void ShowSceneSettingPanel()
    {
        SetAllPanelsInactive();
        if (sceneSettingPanel != null) sceneSettingPanel.SetActive(true);
    }

    public void ShowGameSettingPanel()
    {
        SetAllPanelsInactive();
        if (gameSettingPanel != null) gameSettingPanel.SetActive(true);
    }

    public void ShowDebugInfoPanel()
    {
        SetAllPanelsInactive();
        if (debugInfoDetailPanel != null) debugInfoDetailPanel.SetActive(true);
    }


    /// <summary>
    /// すべてのパネルを非表示にする
    /// </summary>
    private void SetAllPanelsInactive()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (sceneSettingPanel != null) sceneSettingPanel.SetActive(false);
        if (gameSettingPanel != null) gameSettingPanel.SetActive(false);
        if (debugInfoDetailPanel != null) debugInfoDetailPanel.SetActive(false);
    }
}
