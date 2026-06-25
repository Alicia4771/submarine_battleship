using UnityEngine;

public class GameSettingPanel : MonoBehaviour
{
    [SerializeField, Tooltip("ゲーム設定画面の最初の画面")]
    private GameObject gameSettingMainPanel;

    [SerializeField, Tooltip("タイムスケールを操作する画面")]
    private GameObject timeScalePanel;

    [SerializeField, Tooltip("ソナーの設定画面")]
    private GameObject sonarSettingPanel;

    void OnEnable()
    {
        // ゲーム設定画面を開いたときは、必ずメイン画面から表示する
        ShowGameSettingMainPanel();
    }

    public void ShowGameSettingMainPanel()
    {
        SetAllPanelsInactive();

        if (gameSettingMainPanel != null)
        {
            gameSettingMainPanel.SetActive(true);
        }
    }

    public void ShowTimeScalePanel()
    {
        SetAllPanelsInactive();

        if (timeScalePanel != null)
        {
            timeScalePanel.SetActive(true);
        }
    }

    public void ShowSonarSettingPanel()
    {
        SetAllPanelsInactive();

        if (sonarSettingPanel != null)
        {
            sonarSettingPanel.SetActive(true);
        }
    }



    /// <summary>
    /// ゲーム設定パネル内のすべての画面を非表示にする
    /// </summary>
    private void SetAllPanelsInactive()
    {
        if (gameSettingMainPanel != null) gameSettingMainPanel.SetActive(false);
        if (timeScalePanel != null) timeScalePanel.SetActive(false);
        if (sonarSettingPanel != null) sonarSettingPanel.SetActive(false);
    }
}