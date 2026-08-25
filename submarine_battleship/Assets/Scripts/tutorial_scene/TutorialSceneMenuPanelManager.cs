using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialSceneMenuPanelManager : MonoBehaviour
{
    // =========================
    // ソナーUI
    // =========================

    [Header("ソナーUI")]

    [SerializeField, Tooltip("ソナー表示用のパネル")]
    private GameObject sonarPanel;


    // =========================
    // 潜望鏡
    // =========================

    [Header("潜望鏡")]

    [SerializeField, Tooltip(
        "上下移動する潜望鏡の親オブジェクト。未設定の場合はDataManagerの位置を使用")]
    private Transform periscopeTransform;


    [SerializeField, Tooltip(
        "このY座標より低い場合に水中と判定")]
    private float underwaterYThreshold = 0f;


    // =========================
    // ソナー使用条件
    // =========================

    [Header("ソナー使用条件")]

    [SerializeField, Tooltip(
        "ONの場合、水上でも水中でもソナーを開ける")]
    private bool sonarPanelUnderwaterCanOpen = false;


    [SerializeField, Tooltip(
        "ソナーの入力を受け付けるか。チュートリアル開始時はOFF")]
    private bool sonarInputEnabled = false;


    // =========================
    // デバッグ
    // =========================

    [Header("デバッグ")]

    [SerializeField, Tooltip(
        "Unity上でSpaceキーによるソナー操作を許可する")]
    private bool allowKeyboardTestInput = true;


    // =========================
    // 状態
    // =========================

    // 現在ソナーパネルが表示されているか
    private bool isSonarPanelOpen = false;


    // =========================
    // Awake
    // =========================

    private void Awake()
    {
        // ゲーム開始時は必ず閉じる
        SetSonarPanel(false);
    }


    // =========================
    // Update
    // =========================

    private void Update()
    {
        UpdateSonarPanel();
    }


    // =========================
    // OnDisable
    // =========================

    private void OnDisable()
    {
        // この管理スクリプトが無効になった場合も閉じる
        SetSonarPanel(false);
    }


    // ============================================================
    // ソナーパネル更新
    // ============================================================

    /// <summary>
    /// ソナーパネルの表示状態を更新する。
    /// </summary>
    private void UpdateSonarPanel()
    {
        if (sonarPanel == null)
        {
            return;
        }


        // =========================
        // ソナー入力が無効
        // =========================

        // チュートリアルの説明前など、
        // ソナー入力が無効になっている場合
        if (!sonarInputEnabled)
        {
            SetSonarPanel(false);

            return;
        }


        // =========================
        // 潜望鏡の位置確認
        // =========================

        // 潜望鏡が水中になければ表示しない
        if (!CanOpenSonarByPeriscopePosition())
        {
            SetSonarPanel(false);

            return;
        }


        // =========================
        // 入力取得
        // =========================

        bool sonarButtonPressed =
            IsSonarButtonPressed();


        bool spaceKeyPressed =
            allowKeyboardTestInput &&
            IsSpaceKeyPressed();


        // Button1またはSpaceキーを
        // 押している間だけ表示する
        bool shouldOpen =
            sonarButtonPressed ||
            spaceKeyPressed;


        SetSonarPanel(
            shouldOpen
        );
    }


    // ============================================================
    // ソナーボタン
    // ============================================================

    /// <summary>
    /// ソナー用のButton1が押されているか。
    /// </summary>
    private bool IsSonarButtonPressed()
    {
        return
            DataManager.GetSensorButton1() == 1;
    }


    // ============================================================
    // Spaceキー
    // ============================================================

    /// <summary>
    /// テスト用のSpaceキーが押されているか。
    /// </summary>
    private bool IsSpaceKeyPressed()
    {
        return
            Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed;
    }


    // ============================================================
    // ソナーを開ける高さか判定
    // ============================================================

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


        return
            currentY < underwaterYThreshold;
    }


    // ============================================================
    // ソナーパネル表示設定
    // ============================================================

    /// <summary>
    /// ソナーパネルの表示状態を変更する。
    /// </summary>
    private void SetSonarPanel(
        bool isOpen
    )
    {
        isSonarPanelOpen =
            isOpen;


        if (
            sonarPanel != null &&
            sonarPanel.activeSelf != isOpen
        )
        {
            sonarPanel.SetActive(
                isOpen
            );
        }
    }


    // ============================================================
    // ソナーパネル状態取得
    // ============================================================

    /// <summary>
    /// 現在ソナーパネルが表示されているか取得する。
    /// TutorialSceneManagerから使用する。
    /// </summary>
    public bool GetIsSonarPanelOpen()
    {
        return isSonarPanelOpen;
    }


    // ============================================================
    // ソナー入力状態取得
    // ============================================================

    /// <summary>
    /// ソナー入力が有効か取得する。
    /// </summary>
    public bool GetSonarInputEnabled()
    {
        return sonarInputEnabled;
    }


    // ============================================================
    // ソナー入力の有効・無効
    // ============================================================

    /// <summary>
    /// ソナー入力の有効・無効を変更する。
    /// TutorialSceneManagerから使用する。
    /// </summary>
    public void SetSonarInputEnabled(
        bool enabled
    )
    {
        sonarInputEnabled =
            enabled;


        if (!sonarInputEnabled)
        {
            SetSonarPanel(false);
        }
    }


    // ============================================================
    // 水上でのソナー使用設定取得
    // ============================================================

    /// <summary>
    /// 水上でもソナーを使用できる設定か取得する。
    /// </summary>
    public bool GetSonarPanelUnderwaterCanOpen()
    {
        return
            sonarPanelUnderwaterCanOpen;
    }


    // ============================================================
    // 水上でのソナー使用設定
    // ============================================================

    /// <summary>
    /// 水上でもソナーを使用できるか設定する。
    /// </summary>
    public void SetSonarPanelUnderwaterCanOpen(
        bool canOpen
    )
    {
        sonarPanelUnderwaterCanOpen =
            canOpen;


        if (!CanOpenSonarByPeriscopePosition())
        {
            SetSonarPanel(false);
        }
    }


    // ============================================================
    // ソナーパネルを強制的に閉じる
    // ============================================================

    /// <summary>
    /// ソナーパネルを強制的に閉じる。
    /// </summary>
    public void CloseSonarPanel()
    {
        SetSonarPanel(false);
    }
}