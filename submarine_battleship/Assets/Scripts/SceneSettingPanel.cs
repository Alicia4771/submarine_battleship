using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneSettingPanel : MonoBehaviour
{
    [SerializeField, Tooltip("シーン設定画面の最初の画面")]
    private GameObject sceneSettingMainPanel;

    [SerializeField, Tooltip("実際にシーンを操作する画面")]
    private GameObject sceneOperationPanel;

    [Header("確認ダイアログ")]
    [SerializeField, Tooltip("シーン移動確認用のパネル")]
    private GameObject sceneChangeConfirmPanel;

    [SerializeField, Tooltip("確認メッセージを表示するTextMeshPro")]
    private TextMeshProUGUI sceneChangeConfirmMessageText;

    private bool changeSceneConfirmation;    // シーンを変更する際の確認ダイアログを表示するか

    private string startSceneName = "StartScene";
    private string tutorialSceneName = "TutorialScene";
    private string mainSceneName = "MainScene";
    private string resultSceneName = "ResultScene";

    private string pendingSceneName = "";

    void OnEnable()
    {
        // シーン設定画面を開いたときは、必ずメイン画面から表示する
        ShowSceneSettingMainPanel();

        // 確認ダイアログは閉じておく
        HideSceneChangeConfirmPanel();

        changeSceneConfirmation = DataManager.GetChangeSceneConfirmation();
    }

    public void ShowSceneSettingMainPanel()
    {
        SetAllPanelsInactive();

        if (sceneSettingMainPanel != null)
        {
            sceneSettingMainPanel.SetActive(true);
        }
    }

    public void ShowSceneOperationPanel()
    {
        SetAllPanelsInactive();

        if (sceneOperationPanel != null)
        {
            sceneOperationPanel.SetActive(true);
        }
    }

    public void RequestChangeToStartScene()
    {
        RequestChangeScene(startSceneName);
    }

    public void RequestChangeToTutorialScene()
    {
        RequestChangeScene(tutorialSceneName);
    }

    public void RequestChangeToMainScene()
    {
        RequestChangeScene(mainSceneName);
    }

    public void RequestChangeToResultScene()
    {
        RequestChangeScene(resultSceneName);
    }

    private void RequestChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("移動先のシーン名が空です。");
            return;
        }

        // 確認なしなら、そのまま移動
        if (!changeSceneConfirmation)
        {
            ChangeScene(sceneName);
            return;
        }

        // 「はい」を押したときに移動するシーン名を保存
        pendingSceneName = sceneName;

        if (sceneChangeConfirmMessageText != null)
        {
            sceneChangeConfirmMessageText.text =
                sceneName + " に移動しますか？";
        }

        if (sceneChangeConfirmPanel != null)
        {
            sceneChangeConfirmPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("SceneChangeConfirmPanel が設定されていません。");
        }
    }

    public void ConfirmSceneChange()
    {
        if (string.IsNullOrWhiteSpace(pendingSceneName))
        {
            HideSceneChangeConfirmPanel();
            return;
        }

        string sceneName = pendingSceneName;
        pendingSceneName = "";

        HideSceneChangeConfirmPanel();

        ChangeScene(sceneName);
    }

    public void CancelSceneChange()
    {
        pendingSceneName = "";
        HideSceneChangeConfirmPanel();
    }

    private void ChangeScene(string sceneName)
    {
        // 管理者メニューを開いていると Time.timeScale = 0 の可能性があるので戻す
        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
    }

    private void HideSceneChangeConfirmPanel()
    {
        if (sceneChangeConfirmPanel != null)
        {
            sceneChangeConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// シーン設定パネル内の通常画面をすべて非表示にする
    /// </summary>
    private void SetAllPanelsInactive()
    {
        if (sceneSettingMainPanel != null) sceneSettingMainPanel.SetActive(false);
        if (sceneOperationPanel != null) sceneOperationPanel.SetActive(false);
    }
}