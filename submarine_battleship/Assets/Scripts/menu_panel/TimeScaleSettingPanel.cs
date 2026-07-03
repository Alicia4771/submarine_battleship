using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeScaleSettingPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField, Tooltip("TimeScaleを入力するInputField")]
    private TMP_InputField timeScaleInputField;

    [SerializeField, Tooltip("TimeScaleを操作するSlider")]
    private Slider timeScaleSlider;

    [Header("確認ダイアログ")]
    [SerializeField, Tooltip("TimeScale変更確認用パネル")]
    private GameObject confirmPanel;

    [SerializeField, Tooltip("確認メッセージを表示するTextMeshPro")]
    private TextMeshProUGUI confirmMessageText;

    [Header("設定")]
    [SerializeField, Tooltip("TimeScaleの最小値")]
    private float minTimeScale = 0f;

    [SerializeField, Tooltip("TimeScaleの最大値")]
    private float maxTimeScale = 100f;

    [SerializeField, Tooltip("標準ボタンを押したときの値")]
    private float defaultTimeScale = 1f;

    private float pendingTimeScale = 1f;

    // Slider変更 → InputField変更 → Slider変更... となるのを防ぐフラグ
    private bool isUpdatingUI = false;

    void OnEnable()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (timeScaleSlider != null)
        {
            timeScaleSlider.minValue = minTimeScale;
            timeScaleSlider.maxValue = maxTimeScale;
            timeScaleSlider.wholeNumbers = false;

            timeScaleSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            timeScaleSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (timeScaleInputField != null)
        {
            timeScaleInputField.onValueChanged.RemoveListener(OnInputValueChanged);
            timeScaleInputField.onValueChanged.AddListener(OnInputValueChanged);
        }

        // 現在設定されているTimeScaleをUIに反映
        SetUIValue(MenuPanelManager.GetGameTimeScale());

        HideConfirmPanel();
    }

    /// <summary>
    /// Sliderを動かしたときに呼ばれる
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        if (isUpdatingUI) return;

        value = Mathf.Clamp(value, minTimeScale, maxTimeScale);

        isUpdatingUI = true;

        if (timeScaleInputField != null)
        {
            timeScaleInputField.SetTextWithoutNotify(FormatTimeScale(value));
        }

        isUpdatingUI = false;
    }

    /// <summary>
    /// InputFieldに入力したときに呼ばれる
    /// </summary>
    private void OnInputValueChanged(string text)
    {
        if (isUpdatingUI) return;

        // 入力途中の空欄は無視
        if (string.IsNullOrWhiteSpace(text)) return;

        // 小数点だけなど、まだ数値として成立していない入力は無視
        if (text == "." || text == "-") return;

        if (!TryParseTimeScale(text, out float value)) return;

        value = Mathf.Clamp(value, minTimeScale, maxTimeScale);

        isUpdatingUI = true;

        if (timeScaleSlider != null)
        {
            timeScaleSlider.SetValueWithoutNotify(value);
        }

        isUpdatingUI = false;
    }

    /// <summary>
    /// 標準ボタン用
    /// InputFieldとSliderを1に戻す
    /// </summary>
    public void SetDefaultTimeScale()
    {
        SetUIValue(defaultTimeScale);
    }

    /// <summary>
    /// 決定ボタン用
    /// すぐには反映せず、確認ダイアログを表示する
    /// </summary>
    public void RequestApplyTimeScale()
    {
        float value = GetInputTimeScale();
        value = Mathf.Clamp(value, minTimeScale, maxTimeScale);

        pendingTimeScale = value;

        // 範囲外の値が入力されていた場合、UI側も補正後の値にする
        SetUIValue(value);

        if (confirmMessageText != null)
        {
            confirmMessageText.text =
                "TimeScaleを " + FormatTimeScale(value) + " に変更しますか？";
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("ConfirmPanel が設定されていません。");
        }
    }

    /// <summary>
    /// 確認ダイアログのYesボタン用
    /// </summary>
    public void ConfirmApplyTimeScale()
    {
        bool result = MenuPanelManager.SetGameTimeScale(pendingTimeScale);

        if (!result)
        {
            Debug.LogError("TimeScaleの設定に失敗しました: " + pendingTimeScale);
            return;
        }

        HideConfirmPanel();

        Debug.Log("TimeScaleを変更しました: " + pendingTimeScale);
    }

    /// <summary>
    /// 確認ダイアログのNoボタン用
    /// </summary>
    public void CancelApplyTimeScale()
    {
        HideConfirmPanel();

        // キャンセルしたら、現在反映済みのTimeScaleに戻す
        SetUIValue(MenuPanelManager.GetGameTimeScale());
    }

    /// <summary>
    /// SliderとInputFieldの表示値を同時に変更する
    /// </summary>
    private void SetUIValue(float value)
    {
        value = Mathf.Clamp(value, minTimeScale, maxTimeScale);

        isUpdatingUI = true;

        if (timeScaleSlider != null)
        {
            timeScaleSlider.SetValueWithoutNotify(value);
        }

        if (timeScaleInputField != null)
        {
            timeScaleInputField.SetTextWithoutNotify(FormatTimeScale(value));
        }

        isUpdatingUI = false;
    }

    /// <summary>
    /// InputFieldからTimeScale値を取得する
    /// </summary>
    private float GetInputTimeScale()
    {
        if (timeScaleInputField == null)
        {
            return MenuPanelManager.GetGameTimeScale();
        }

        if (TryParseTimeScale(timeScaleInputField.text, out float value))
        {
            return value;
        }

        Debug.LogWarning("TimeScaleの入力値が不正です。現在のTimeScaleを使用します。");

        return MenuPanelManager.GetGameTimeScale();
    }

    /// <summary>
    /// stringをfloatに変換する
    /// </summary>
    private bool TryParseTimeScale(string text, out float value)
    {
        return float.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value
        );
    }

    /// <summary>
    /// TimeScaleの表示形式を整える
    /// </summary>
    private string FormatTimeScale(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void HideConfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }
}