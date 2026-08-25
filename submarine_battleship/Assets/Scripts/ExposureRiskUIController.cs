using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExposureRiskUIController : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float MinimumNormalizedValue =
        0.0f;

    private const float MaximumNormalizedValue =
        1.0f;

    private const float PercentageMultiplier =
        100.0f;


    // ============================================================
    // デフォルトしきい値
    // ============================================================

    private const float DefaultCautionThreshold =
        0.50f;

    private const float DefaultDangerThreshold =
        0.75f;

    private const float DefaultCriticalThreshold =
        0.90f;


    // ============================================================
    // アニメーション
    // ============================================================

    private const float DefaultFillChangeSpeed =
        2.5f;

    private const float DefaultDetectionWarningDuration =
        2.0f;


    // ============================================================
    // 表示文字
    // ============================================================

    private const string DefaultRiskTitle =
        "危険度";

    private const string SafeStateText =
        "安全";

    private const string RecoveringStateText =
        "危険度低下中";

    private const string PeriscopeExposureText =
        "潜望鏡露出中";

    private const string MastExposureText =
        "通信マスト露出中";

    private const string BothExposureText =
        "潜望鏡・通信マスト露出中";

    private const string DisabledStateText =
        "危険度システム停止";

    private const string DetectionWarningMessage =
        "敵に発見された！";


    // ============================================================
    // デフォルト色
    // ============================================================

    private static readonly Color DefaultSafeColor =
        Color.green;

    private static readonly Color DefaultCautionColor =
        Color.yellow;

    private static readonly Color DefaultDangerColor =
        new Color(
            1.0f,
            0.5f,
            0.0f,
            1.0f
        );

    private static readonly Color DefaultCriticalColor =
        Color.red;


    // ============================================================
    // Risk Manager
    // ============================================================

    [Header("Risk Manager")]

    [SerializeField, Tooltip(
        "危険度を管理するExposureRiskManager。" +
        "未設定の場合はシーン内から自動検索する")]
    private ExposureRiskManager
        exposureRiskManager;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "危険度ゲージのFill Image")]
    private Image
        riskBarFill;


    [SerializeField, Tooltip(
        "危険度の数値を表示するTextMeshPro")]
    private TMP_Text
        riskValueText;


    [SerializeField, Tooltip(
        "危険度の状態を表示するTextMeshPro")]
    private TMP_Text
        riskStatusText;


    [SerializeField, Tooltip(
        "危険度タイトル。" +
        "不要な場合は未設定でもよい")]
    private TMP_Text
        riskTitleText;


    [SerializeField, Tooltip(
        "敵に発見された時の警告Text。" +
        "不要な場合は未設定でもよい")]
    private TMP_Text
        detectionWarningText;


    // ============================================================
    // 表示設定
    // ============================================================

    [Header("Display")]

    [SerializeField, Tooltip(
        "数値表示にパーセントも表示する")]
    private bool showPercentage =
        true;


    [SerializeField, Tooltip(
        "Detection Thresholdをゲージ100%として扱う。" +
        "OFFの場合はMaximum Riskを100%として扱う")]
    private bool normalizeAgainstDetectionThreshold =
        true;


    [SerializeField, Tooltip(
        "潜望鏡・通信マストのどちらが露出しているか表示する")]
    private bool showExposureSource =
        true;


    // ============================================================
    // ゲージアニメーション
    // ============================================================

    [Header("Animation")]

    [SerializeField, Tooltip(
        "ゲージが実際の危険度へ追従する速度。" +
        "0の場合は即座に切り替わる")]
    [Min(MinimumNormalizedValue)]
    private float fillChangeSpeed =
        DefaultFillChangeSpeed;


    // ============================================================
    // 色
    // ============================================================

    [Header("Risk Color")]

    [SerializeField]
    private Color safeColor =
        DefaultSafeColor;


    [SerializeField]
    private Color cautionColor =
        DefaultCautionColor;


    [SerializeField]
    private Color dangerColor =
        DefaultDangerColor;


    [SerializeField]
    private Color criticalColor =
        DefaultCriticalColor;


    // ============================================================
    // 色変更しきい値
    // ============================================================

    [Header("Color Threshold")]

    [SerializeField, Tooltip(
        "この割合から注意色へ変化する")]
    [Range(
        MinimumNormalizedValue,
        MaximumNormalizedValue
    )]
    private float cautionThreshold =
        DefaultCautionThreshold;


    [SerializeField, Tooltip(
        "この割合から危険色へ変化する")]
    [Range(
        MinimumNormalizedValue,
        MaximumNormalizedValue
    )]
    private float dangerThreshold =
        DefaultDangerThreshold;


    [SerializeField, Tooltip(
        "この割合から発見寸前色へ変化する")]
    [Range(
        MinimumNormalizedValue,
        MaximumNormalizedValue
    )]
    private float criticalThreshold =
        DefaultCriticalThreshold;


    // ============================================================
    // 発見警告
    // ============================================================

    [Header("Detection Warning")]

    [SerializeField, Tooltip(
        "敵に発見された時の警告表示時間")]
    [Min(MinimumNormalizedValue)]
    private float detectionWarningDuration =
        DefaultDetectionWarningDuration;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // 内部状態
    // ============================================================

    private float targetNormalizedRisk =
        MinimumNormalizedValue;


    private float displayedNormalizedRisk =
        MinimumNormalizedValue;


    private Coroutine
        detectionWarningCoroutine;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        ValidateSettings();

        InitializeUI();
    }


    // ============================================================
    // OnEnable
    // ============================================================

    private void OnEnable()
    {
        ResolveReferences();

        SubscribeEvents();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        RefreshAll();
    }


    // ============================================================
    // OnDisable
    // ============================================================

    private void OnDisable()
    {
        UnsubscribeEvents();


        if (
            detectionWarningCoroutine !=
            null
        )
        {
            StopCoroutine(
                detectionWarningCoroutine
            );


            detectionWarningCoroutine =
                null;
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        UpdateRiskBarAnimation();

        UpdateStatusText();
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (
            exposureRiskManager !=
            null
        )
        {
            return;
        }


        exposureRiskManager =
            FindFirstObjectByType<
                ExposureRiskManager
            >();


        if (
            exposureRiskManager ==
                null
            &&
            debugLog
        )
        {
            Debug.LogWarning(
                "ExposureRiskUIController: " +
                "ExposureRiskManagerが見つかりません。"
            );
        }
    }


    // ============================================================
    // Event登録
    // ============================================================

    private void SubscribeEvents()
    {
        if (
            exposureRiskManager ==
            null
        )
        {
            return;
        }


        // 二重登録を防ぐ
        exposureRiskManager
            .RiskChanged -=
                HandleRiskChanged;


        exposureRiskManager
            .RiskChanged +=
                HandleRiskChanged;


        exposureRiskManager
            .EnemyDetectionTriggered -=
                HandleEnemyDetectionTriggered;


        exposureRiskManager
            .EnemyDetectionTriggered +=
                HandleEnemyDetectionTriggered;
    }


    // ============================================================
    // Event解除
    // ============================================================

    private void UnsubscribeEvents()
    {
        if (
            exposureRiskManager ==
            null
        )
        {
            return;
        }


        exposureRiskManager
            .RiskChanged -=
                HandleRiskChanged;


        exposureRiskManager
            .EnemyDetectionTriggered -=
                HandleEnemyDetectionTriggered;
    }


    // ============================================================
    // UI初期化
    // ============================================================

    private void InitializeUI()
    {
        if (riskTitleText != null)
        {
            riskTitleText.text =
                DefaultRiskTitle;
        }


        if (riskBarFill != null)
        {
            riskBarFill.fillAmount =
                MinimumNormalizedValue;


            riskBarFill.color =
                safeColor;
        }


        if (riskValueText != null)
        {
            riskValueText.text =
                BuildRiskValueText(
                    MinimumNormalizedValue
                );
        }


        if (riskStatusText != null)
        {
            riskStatusText.text =
                SafeStateText;
        }


        if (
            detectionWarningText !=
            null
        )
        {
            detectionWarningText.text =
                DetectionWarningMessage;


            detectionWarningText
                .gameObject
                .SetActive(
                    false
                );
        }
    }


    // ============================================================
    // 全体更新
    // ============================================================

    private void RefreshAll()
    {
        if (
            exposureRiskManager ==
            null
        )
        {
            ResolveReferences();
        }


        if (
            exposureRiskManager ==
            null
        )
        {
            return;
        }


        float currentRisk =
            exposureRiskManager
                .GetCurrentRisk();


        targetNormalizedRisk =
            CalculateNormalizedRisk(
                currentRisk
            );


        displayedNormalizedRisk =
            targetNormalizedRisk;


        ApplyRiskBar(
            displayedNormalizedRisk
        );


        UpdateRiskValueText(
            currentRisk
        );


        UpdateStatusText();
    }


    // ============================================================
    // Risk Changed
    // ============================================================

    private void HandleRiskChanged(
        float currentRisk
    )
    {
        if (
            exposureRiskManager ==
            null
        )
        {
            return;
        }


        targetNormalizedRisk =
            CalculateNormalizedRisk(
                currentRisk
            );


        UpdateRiskValueText(
            currentRisk
        );


        if (debugLog)
        {
            Debug.Log(
                "Risk UI更新: " +
                currentRisk
            );
        }
    }


    // ============================================================
    // 敵発見
    // ============================================================

    private void HandleEnemyDetectionTriggered(
        int detectionCount
    )
    {
        if (
            detectionWarningText ==
            null
        )
        {
            return;
        }


        if (
            detectionWarningCoroutine !=
            null
        )
        {
            StopCoroutine(
                detectionWarningCoroutine
            );
        }


        detectionWarningCoroutine =
            StartCoroutine(
                ShowDetectionWarningRoutine()
            );
    }


    // ============================================================
    // 発見警告
    // ============================================================

    private IEnumerator
        ShowDetectionWarningRoutine()
    {
        if (
            detectionWarningText ==
            null
        )
        {
            yield break;
        }


        detectionWarningText.text =
            DetectionWarningMessage;


        detectionWarningText
            .gameObject
            .SetActive(
                true
            );


        if (
            detectionWarningDuration >
            MinimumNormalizedValue
        )
        {
            yield return
                new WaitForSecondsRealtime(
                    detectionWarningDuration
                );
        }


        detectionWarningText
            .gameObject
            .SetActive(
                false
            );


        detectionWarningCoroutine =
            null;
    }


    // ============================================================
    // Risk正規化
    // ============================================================

    private float CalculateNormalizedRisk(
        float currentRisk
    )
    {
        if (
            exposureRiskManager ==
            null
        )
        {
            return
                MinimumNormalizedValue;
        }


        float referenceRisk =
            GetReferenceRisk();


        if (
            referenceRisk <=
            Mathf.Epsilon
        )
        {
            return
                MinimumNormalizedValue;
        }


        return
            Mathf.Clamp01(
                currentRisk /
                referenceRisk
            );
    }


    // ============================================================
    // 基準危険度
    // ============================================================

    private float GetReferenceRisk()
    {
        if (
            exposureRiskManager ==
            null
        )
        {
            return
                MaximumNormalizedValue;
        }


        if (
            normalizeAgainstDetectionThreshold
        )
        {
            return
                exposureRiskManager
                    .GetDetectionThreshold();
        }


        return
            exposureRiskManager
                .GetMaximumRisk();
    }


    // ============================================================
    // ゲージアニメーション
    // ============================================================

    private void UpdateRiskBarAnimation()
    {
        if (riskBarFill == null)
        {
            return;
        }


        if (
            fillChangeSpeed <=
            MinimumNormalizedValue
        )
        {
            displayedNormalizedRisk =
                targetNormalizedRisk;
        }
        else
        {
            displayedNormalizedRisk =
                Mathf.MoveTowards(
                    displayedNormalizedRisk,
                    targetNormalizedRisk,
                    fillChangeSpeed *
                    Time.unscaledDeltaTime
                );
        }


        ApplyRiskBar(
            displayedNormalizedRisk
        );
    }


    // ============================================================
    // ゲージ反映
    // ============================================================

    private void ApplyRiskBar(
        float normalizedRisk
    )
    {
        if (riskBarFill == null)
        {
            return;
        }


        float clampedRisk =
            Mathf.Clamp01(
                normalizedRisk
            );


        riskBarFill.fillAmount =
            clampedRisk;


        riskBarFill.color =
            CalculateRiskColor(
                clampedRisk
            );
    }


    // ============================================================
    // 危険度色
    // ============================================================

    private Color CalculateRiskColor(
        float normalizedRisk
    )
    {
        // =========================
        // Safe → Caution
        // =========================

        if (
            normalizedRisk <
            cautionThreshold
        )
        {
            float interpolation =
                Mathf.InverseLerp(
                    MinimumNormalizedValue,
                    cautionThreshold,
                    normalizedRisk
                );


            return
                Color.Lerp(
                    safeColor,
                    cautionColor,
                    interpolation
                );
        }


        // =========================
        // Caution → Danger
        // =========================

        if (
            normalizedRisk <
            dangerThreshold
        )
        {
            float interpolation =
                Mathf.InverseLerp(
                    cautionThreshold,
                    dangerThreshold,
                    normalizedRisk
                );


            return
                Color.Lerp(
                    cautionColor,
                    dangerColor,
                    interpolation
                );
        }


        // =========================
        // Danger → Critical
        // =========================

        if (
            normalizedRisk <
            criticalThreshold
        )
        {
            float interpolation =
                Mathf.InverseLerp(
                    dangerThreshold,
                    criticalThreshold,
                    normalizedRisk
                );


            return
                Color.Lerp(
                    dangerColor,
                    criticalColor,
                    interpolation
                );
        }


        return
            criticalColor;
    }


    // ============================================================
    // 数値表示
    // ============================================================

    private void UpdateRiskValueText(
        float currentRisk
    )
    {
        if (riskValueText == null)
        {
            return;
        }


        riskValueText.text =
            BuildRiskValueText(
                currentRisk
            );
    }


    // ============================================================
    // 数値文字列
    // ============================================================

    private string BuildRiskValueText(
        float currentRisk
    )
    {
        float referenceRisk;


        if (
            exposureRiskManager !=
            null
        )
        {
            referenceRisk =
                GetReferenceRisk();
        }
        else
        {
            referenceRisk =
                PercentageMultiplier;
        }


        if (!showPercentage)
        {
            return
                currentRisk.ToString("0") +
                " / " +
                referenceRisk.ToString("0");
        }


        float normalizedRisk =
            referenceRisk >
                Mathf.Epsilon
                    ? Mathf.Clamp01(
                        currentRisk /
                        referenceRisk
                    )
                    : MinimumNormalizedValue;


        float percentage =
            normalizedRisk *
            PercentageMultiplier;


        return
            currentRisk.ToString("0") +
            " / " +
            referenceRisk.ToString("0") +
            " (" +
            percentage.ToString("0") +
            "%)";
    }


    // ============================================================
    // 状態表示
    // ============================================================

    private void UpdateStatusText()
    {
        if (
            riskStatusText == null ||
            exposureRiskManager == null
        )
        {
            return;
        }


        // =========================
        // システム停止中
        // =========================

        if (
            !exposureRiskManager
                .GetRiskSystemEnabled()
        )
        {
            riskStatusText.text =
                DisabledStateText;


            return;
        }


        bool periscopeExposed =
            exposureRiskManager
                .GetIsPeriscopeExposed();


        bool mastExposed =
            exposureRiskManager
                .GetIsCommunicationMastExposed();


        // =========================
        // 露出元
        // =========================

        if (showExposureSource)
        {
            if (
                periscopeExposed &&
                mastExposed
            )
            {
                riskStatusText.text =
                    BothExposureText;


                return;
            }


            if (periscopeExposed)
            {
                riskStatusText.text =
                    PeriscopeExposureText;


                return;
            }


            if (mastExposed)
            {
                riskStatusText.text =
                    MastExposureText;


                return;
            }
        }


        // =========================
        // 露出していない場合
        // =========================

        float currentRisk =
            exposureRiskManager
                .GetCurrentRisk();


        if (
            currentRisk >
            MinimumNormalizedValue
        )
        {
            riskStatusText.text =
                RecoveringStateText;
        }
        else
        {
            riskStatusText.text =
                SafeStateText;
        }
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    // ============================================================
    // 設定値検証
    // ============================================================

    private void ValidateSettings()
    {
        fillChangeSpeed =
            Mathf.Max(
                MinimumNormalizedValue,
                fillChangeSpeed
            );


        detectionWarningDuration =
            Mathf.Max(
                MinimumNormalizedValue,
                detectionWarningDuration
            );


        cautionThreshold =
            Mathf.Clamp01(
                cautionThreshold
            );


        dangerThreshold =
            Mathf.Clamp(
                dangerThreshold,
                cautionThreshold,
                MaximumNormalizedValue
            );


        criticalThreshold =
            Mathf.Clamp(
                criticalThreshold,
                dangerThreshold,
                MaximumNormalizedValue
            );
    }
}