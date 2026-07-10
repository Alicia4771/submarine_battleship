using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialSceneMenuPanelManager : MonoBehaviour
{
    [Header("ソナーUI")]
    [SerializeField, Tooltip("ソナー表示用のパネル")]
    private GameObject sonarPanel;

    [Header("センサ")]
    [SerializeField, Tooltip("センサー読み取り用スクリプト")]
    private SensorRead sensorRead;

    [Header("潜望鏡")]
    [SerializeField, Tooltip(
        "上下移動する潜望鏡の親オブジェクト。未設定の場合はDataManagerの位置を使用")]
    private Transform periscopeTransform;

    [SerializeField, Tooltip(
        "このY座標より低い場合に水中と判定")]
    private float underwaterYThreshold = 0f;

    [Header("ソナー使用条件")]
    [SerializeField, Tooltip(
        "ONの場合、水上でも水中でもソナーを開ける")]
    private bool sonarPanelUnderwaterCanOpen = false;

    [SerializeField, Tooltip(
        "ソナーの入力を受け付けるか。チュートリアル開始時はOFF")]
    private bool sonarInputEnabled = false;

    [Header("デバッグ")]
    [SerializeField, Tooltip(
        "Unity上でSpaceキーによるソナー操作を許可する")]
    private bool allowKeyboardTestInput = true;

    // 現在ソナーパネルが表示されているか
    private bool isSonarPanelOpen = false;

    private void Awake()
    {
        if (sensorRead == null)
        {
            sensorRead =
                FindFirstObjectByType<SensorRead>();
        }

        // ゲーム開始時は必ず閉じる
        SetSonarPanel(false);
    }

    private void Update()
    {
        UpdateSonarPanel();
    }

    private void OnDisable()
    {
        // この管理スクリプトが無効になった場合も閉じる
        SetSonarPanel(false);
    }

    /// <summary>
    /// ソナーパネルの表示状態を更新する。
    /// </summary>
    private void UpdateSonarPanel()
    {
        if (sonarPanel == null)
        {
            return;
        }

        // チュートリアルの説明前など、
        // ソナー入力が無効になっている場合
        if (!sonarInputEnabled)
        {
            SetSonarPanel(false);
            return;
        }

        // 潜望鏡が水中になければ表示しない
        if (!CanOpenSonarByPeriscopePosition())
        {
            SetSonarPanel(false);
            return;
        }

        bool tactileSwitchPressed =
            IsTactileSwitchPressed();

        bool spaceKeyPressed =
            allowKeyboardTestInput &&
            IsSpaceKeyPressed();

        // タクトスイッチまたはSpaceキーを
        // 押している間だけ表示する
        bool shouldOpen =
            tactileSwitchPressed ||
            spaceKeyPressed;

        SetSonarPanel(shouldOpen);
    }

    /// <summary>
    /// タクトスイッチが押されているか。
    /// </summary>
    private bool IsTactileSwitchPressed()
    {
        if (sensorRead == null)
        {
            return false;
        }

        return sensorRead.GetTactileSwitch() == 1;
    }

    /// <summary>
    /// テスト用のSpaceキーが押されているか。
    /// </summary>
    private bool IsSpaceKeyPressed()
    {
        return Keyboard.current != null &&
               Keyboard.current.spaceKey.isPressed;
    }

    /// <summary>
    /// 現在の潜望鏡の高さで
    /// ソナーを開けるか判定する。
    /// </summary>
    private bool CanOpenSonarByPeriscopePosition()
    {
        // ONなら潜望鏡の高さに関係なく使用可能
        if (sonarPanelUnderwaterCanOpen)
        {
            return true;
        }

        float currentY;

        if (periscopeTransform != null)
        {
            // TutorialSceneではこちらを使用するのがおすすめ
            currentY =
                periscopeTransform.position.y;
        }
        else
        {
            // Transformが未設定の場合は、
            // DataManagerに保存された位置を使用
            currentY =
                DataManager
                    .GetSubmarinePosition()
                    .y;
        }

        return currentY < underwaterYThreshold;
    }

    /// <summary>
    /// ソナーパネルの表示状態を変更する。
    /// </summary>
    private void SetSonarPanel(bool isOpen)
    {
        isSonarPanelOpen = isOpen;

        if (sonarPanel != null &&
            sonarPanel.activeSelf != isOpen)
        {
            sonarPanel.SetActive(isOpen);
        }
    }

    /// <summary>
    /// 現在ソナーパネルが表示されているか取得する。
    /// TutorialSceneManagerから使用する。
    /// </summary>
    public bool GetIsSonarPanelOpen()
    {
        return isSonarPanelOpen;
    }

    /// <summary>
    /// ソナー入力が有効か取得する。
    /// </summary>
    public bool GetSonarInputEnabled()
    {
        return sonarInputEnabled;
    }

    /// <summary>
    /// ソナー入力の有効・無効を変更する。
    /// TutorialSceneManagerから使用する。
    /// </summary>
    public void SetSonarInputEnabled(bool enabled)
    {
        sonarInputEnabled = enabled;

        if (!sonarInputEnabled)
        {
            SetSonarPanel(false);
        }
    }

    /// <summary>
    /// 水上でもソナーを使用できる設定か取得する。
    /// </summary>
    public bool GetSonarPanelUnderwaterCanOpen()
    {
        return sonarPanelUnderwaterCanOpen;
    }

    /// <summary>
    /// 水上でもソナーを使用できるか設定する。
    /// </summary>
    public void SetSonarPanelUnderwaterCanOpen(
        bool canOpen)
    {
        sonarPanelUnderwaterCanOpen = canOpen;

        if (!CanOpenSonarByPeriscopePosition())
        {
            SetSonarPanel(false);
        }
    }

    /// <summary>
    /// ソナーパネルを強制的に閉じる。
    /// </summary>
    public void CloseSonarPanel()
    {
        SetSonarPanel(false);
    }
}