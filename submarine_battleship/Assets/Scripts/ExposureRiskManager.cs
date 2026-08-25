using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ExposureRiskManager : MonoBehaviour
{
    // ============================================================
    // 危険状態
    // ============================================================

    public enum ExposureRiskState
    {
        Safe,
        Accumulating,
        Recovering,
        DetectionCooldown
    }


    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultMaximumRisk = 100.0f;
    private const float DefaultDetectionThreshold = 100.0f;

    private const float DefaultPeriscopeRiskPerSecond = 12.0f;
    private const float DefaultCommunicationMastRiskPerSecond = 20.0f;

    private const float DefaultSimultaneousExposureMultiplier = 1.0f;

    private const float DefaultRiskRecoveryPerSecond = 10.0f;
    private const float DefaultRecoveryStartDelay = 0.5f;

    private const float DefaultPostDetectionRisk = 35.0f;
    private const float DefaultDetectionCooldownDuration = 3.0f;

    private const int DefaultDetectionScorePenalty = 100;

    private const float MinimumRisk = 0.0f;
    private const float MinimumNonNegativeValue = 0.0f;

    private const float DefaultRiskChangeEventThreshold = 0.01f;


    // ============================================================
    // 通信マスト
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "通信マストを管理するCommunicationMastController。" +
        "未設定の場合はシーン内から自動検索する")]
    private CommunicationMastController
        communicationMastController;


    // ============================================================
    // 危険度
    // ============================================================

    [Header("Risk")]

    [SerializeField, Tooltip(
        "危険度の最大値")]
    [Min(MinimumNonNegativeValue)]
    private float maximumRisk =
        DefaultMaximumRisk;


    [SerializeField, Tooltip(
        "この値以上になると敵に発見されたと判定する")]
    [Min(MinimumNonNegativeValue)]
    private float detectionThreshold =
        DefaultDetectionThreshold;


    [SerializeField, Tooltip(
        "ゲーム開始時の危険度")]
    [Min(MinimumNonNegativeValue)]
    private float initialRisk =
        MinimumRisk;


    // ============================================================
    // 潜望鏡
    // ============================================================

    [Header("Periscope Exposure")]

    [SerializeField, Tooltip(
        "潜望鏡が海面上に出ている間、" +
        "1秒あたりに増加する危険度")]
    [Min(MinimumNonNegativeValue)]
    private float periscopeRiskPerSecond =
        DefaultPeriscopeRiskPerSecond;


    // ============================================================
    // 通信マスト
    // ============================================================

    [Header("Communication Mast Exposure")]

    [SerializeField, Tooltip(
        "通信マストが露出している間、" +
        "1秒あたりに増加する危険度")]
    [Min(MinimumNonNegativeValue)]
    private float communicationMastRiskPerSecond =
        DefaultCommunicationMastRiskPerSecond;


    // ============================================================
    // 同時露出
    // ============================================================

    [Header("Multiple Exposure")]

    [SerializeField, Tooltip(
        "潜望鏡と通信マストが同時に露出した場合の倍率。" +
        "1なら単純加算")]
    [Min(MinimumNonNegativeValue)]
    private float simultaneousExposureMultiplier =
        DefaultSimultaneousExposureMultiplier;


    // ============================================================
    // 危険度回復
    // ============================================================

    [Header("Risk Recovery")]

    [SerializeField, Tooltip(
        "すべての装置を格納した状態で、" +
        "1秒あたりに減少する危険度")]
    [Min(MinimumNonNegativeValue)]
    private float riskRecoveryPerSecond =
        DefaultRiskRecoveryPerSecond;


    [SerializeField, Tooltip(
        "最後の露出終了から危険度減少が始まるまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float recoveryStartDelay =
        DefaultRecoveryStartDelay;


    // ============================================================
    // 発見
    // ============================================================

    [Header("Enemy Detection")]

    [SerializeField, Tooltip(
        "敵に発見された時、危険度をこの値まで戻す")]
    [Min(MinimumNonNegativeValue)]
    private float postDetectionRisk =
        DefaultPostDetectionRisk;


    [SerializeField, Tooltip(
        "発見判定後、次の発見判定を受け付けるまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float detectionCooldownDuration =
        DefaultDetectionCooldownDuration;


    [SerializeField, Tooltip(
        "敵に発見された時にスコアペナルティを適用する")]
    private bool applyScorePenaltyOnDetection =
        true;


    [SerializeField, Tooltip(
        "敵に発見された時に減点するスコア")]
    [Min(0)]
    private int detectionScorePenalty =
        DefaultDetectionScorePenalty;


    // ============================================================
    // 動作設定
    // ============================================================

    [Header("System")]

    [SerializeField, Tooltip(
        "発見危険度システムを有効にする")]
    private bool riskSystemEnabled =
        true;


    // ============================================================
    // デバッグ
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "状態変化や敵発見をConsoleへ表示する")]
    private bool debugLog =
        true;


    [SerializeField, Tooltip(
        "Inspector確認用の現在危険度。" +
        "実行中のみ変化する")]
    private float currentRisk =
        MinimumRisk;


    // ============================================================
    // 内部状態
    // ============================================================

    private ExposureRiskState currentState =
        ExposureRiskState.Safe;


    private bool isPeriscopeExposed = false;

    private bool isCommunicationMastExposed = false;


    private float timeSinceLastExposure =
        0.0f;


    private float detectionCooldownRemaining =
        0.0f;


    private float lastNotifiedRisk =
        MinimumRisk;


    private int detectionCount =
        0;


    // ============================================================
    // イベント
    // ============================================================

    /// <summary>
    /// 危険度が変化した時。
    ///
    /// float:
    /// 現在の危険度
    ///
    /// 将来UIゲージに接続できる。
    /// </summary>
    public event Action<float>
        RiskChanged;


    /// <summary>
    /// 状態が変化した時。
    /// </summary>
    public event Action<ExposureRiskState>
        RiskStateChanged;


    /// <summary>
    /// 敵に発見された時。
    ///
    /// int:
    /// 通算発見回数
    ///
    /// 将来、
    /// 敵攻撃システムや警報演出を
    /// このイベントへ接続できる。
    /// </summary>
    public event Action<int>
        EnemyDetectionTriggered;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        ValidateSettings();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        currentRisk =
            Mathf.Clamp(
                initialRisk,
                MinimumRisk,
                maximumRisk
            );


        lastNotifiedRisk =
            currentRisk;


        timeSinceLastExposure =
            0.0f;


        detectionCooldownRemaining =
            0.0f;


        detectionCount =
            0;


        UpdateExposureSources();


        UpdateState();


        RiskChanged?.Invoke(
            currentRisk
        );
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (!riskSystemEnabled)
        {
            return;
        }


        float deltaTime =
            Time.deltaTime;


        // =========================
        // 発見後クールダウン
        // =========================

        UpdateDetectionCooldown(
            deltaTime
        );


        // =========================
        // 露出状態
        // =========================

        UpdateExposureSources();


        // =========================
        // 危険度更新
        // =========================

        UpdateRisk(
            deltaTime
        );


        // =========================
        // 発見判定
        // =========================

        CheckEnemyDetection();


        // =========================
        // 状態更新
        // =========================

        UpdateState();


        // =========================
        // イベント
        // =========================

        NotifyRiskChangedIfNeeded();
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (
            communicationMastController ==
            null
        )
        {
            communicationMastController =
                FindFirstObjectByType<
                    CommunicationMastController
                >();
        }


        if (
            communicationMastController ==
            null &&
            debugLog
        )
        {
            Debug.LogWarning(
                "CommunicationMastControllerが見つかりません。" +
                "通信マストによる危険度上昇は無効になります。"
            );
        }
    }


    // ============================================================
    // 露出状態
    // ============================================================

    private void UpdateExposureSources()
    {
        // =========================
        // 潜望鏡
        // =========================

        isPeriscopeExposed =
            DataManager
                .GetIsPeriscopeAboveSurface();


        // =========================
        // 通信マスト
        // =========================

        isCommunicationMastExposed =
            communicationMastController !=
                null
            &&
            communicationMastController
                .GetIsMastExposed();
    }


    // ============================================================
    // 危険度更新
    // ============================================================

    private void UpdateRisk(
        float deltaTime
    )
    {
        float riskGainPerSecond =
            CalculateRiskGainPerSecond();


        // ========================================================
        // 露出中
        // ========================================================

        if (
            riskGainPerSecond >
            MinimumNonNegativeValue
        )
        {
            timeSinceLastExposure =
                0.0f;


            AddRisk(
                riskGainPerSecond *
                deltaTime
            );


            return;
        }


        // ========================================================
        // 非露出
        // ========================================================

        timeSinceLastExposure +=
            deltaTime;


        // 回復開始待ち
        if (
            timeSinceLastExposure <
            recoveryStartDelay
        )
        {
            return;
        }


        // 危険度回復
        AddRisk(
            -riskRecoveryPerSecond *
            deltaTime
        );
    }


    // ============================================================
    // 1秒あたり危険度
    // ============================================================

    private float CalculateRiskGainPerSecond()
    {
        float riskGain =
            MinimumRisk;


        int activeExposureSourceCount =
            0;


        // =========================
        // 潜望鏡
        // =========================

        if (isPeriscopeExposed)
        {
            riskGain +=
                periscopeRiskPerSecond;


            activeExposureSourceCount++;
        }


        // =========================
        // 通信マスト
        // =========================

        if (isCommunicationMastExposed)
        {
            riskGain +=
                communicationMastRiskPerSecond;


            activeExposureSourceCount++;
        }


        // =========================
        // 複数装置同時露出
        // =========================

        if (
            activeExposureSourceCount >
            1
        )
        {
            riskGain *=
                simultaneousExposureMultiplier;
        }


        return
            riskGain;
    }


    // ============================================================
    // 危険度加算
    // ============================================================

    private void AddRisk(
        float amount
    )
    {
        currentRisk =
            Mathf.Clamp(
                currentRisk +
                amount,

                MinimumRisk,
                maximumRisk
            );
    }


    // ============================================================
    // 発見判定
    // ============================================================

    private void CheckEnemyDetection()
    {
        // クールダウン中は
        // 新しい発見判定を行わない
        if (
            detectionCooldownRemaining >
            MinimumNonNegativeValue
        )
        {
            return;
        }


        if (
            currentRisk <
            detectionThreshold
        )
        {
            return;
        }


        TriggerEnemyDetection();
    }


    // ============================================================
    // 敵発見
    // ============================================================

    private void TriggerEnemyDetection()
    {
        detectionCount++;


        // =========================
        // ペナルティ
        // =========================

        if (
            applyScorePenaltyOnDetection &&
            detectionScorePenalty >
            0
        )
        {
            DataManager.AddScore(
                -detectionScorePenalty
            );
        }


        // =========================
        // 危険度リセット
        // =========================

        currentRisk =
            Mathf.Clamp(
                postDetectionRisk,
                MinimumRisk,
                maximumRisk
            );


        // =========================
        // クールダウン
        // =========================

        detectionCooldownRemaining =
            detectionCooldownDuration;


        // =========================
        // Console
        // =========================

        if (debugLog)
        {
            if (
                applyScorePenaltyOnDetection &&
                detectionScorePenalty >
                0
            )
            {
                Debug.LogWarning(
                    "敵に発見されました！ -" +
                    detectionScorePenalty +
                    "点"
                );
            }
            else
            {
                Debug.LogWarning(
                    "敵に発見されました！"
                );
            }
        }


        // =========================
        // イベント
        // =========================

        EnemyDetectionTriggered?.Invoke(
            detectionCount
        );


        RiskChanged?.Invoke(
            currentRisk
        );


        lastNotifiedRisk =
            currentRisk;
    }


    // ============================================================
    // 発見クールダウン
    // ============================================================

    private void UpdateDetectionCooldown(
        float deltaTime
    )
    {
        if (
            detectionCooldownRemaining <=
            MinimumNonNegativeValue
        )
        {
            detectionCooldownRemaining =
                MinimumNonNegativeValue;

            return;
        }


        detectionCooldownRemaining -=
            deltaTime;


        if (
            detectionCooldownRemaining <
            MinimumNonNegativeValue
        )
        {
            detectionCooldownRemaining =
                MinimumNonNegativeValue;
        }
    }


    // ============================================================
    // 状態
    // ============================================================

    private void UpdateState()
    {
        ExposureRiskState newState;


        if (
            detectionCooldownRemaining >
            MinimumNonNegativeValue
        )
        {
            newState =
                ExposureRiskState
                    .DetectionCooldown;
        }
        else if (
            isPeriscopeExposed ||
            isCommunicationMastExposed
        )
        {
            newState =
                ExposureRiskState
                    .Accumulating;
        }
        else if (
            currentRisk >
            MinimumRisk
        )
        {
            newState =
                ExposureRiskState
                    .Recovering;
        }
        else
        {
            newState =
                ExposureRiskState
                    .Safe;
        }


        SetState(
            newState
        );
    }


    private void SetState(
        ExposureRiskState newState
    )
    {
        if (
            currentState ==
            newState
        )
        {
            return;
        }


        currentState =
            newState;


        if (debugLog)
        {
            Debug.Log(
                "発見危険度状態: " +
                currentState
            );
        }


        RiskStateChanged?.Invoke(
            currentState
        );
    }


    // ============================================================
    // 危険度変化通知
    // ============================================================

    private void NotifyRiskChangedIfNeeded()
    {
        float difference =
            Mathf.Abs(
                currentRisk -
                lastNotifiedRisk
            );


        if (
            difference <
            DefaultRiskChangeEventThreshold
        )
        {
            return;
        }


        lastNotifiedRisk =
            currentRisk;


        RiskChanged?.Invoke(
            currentRisk
        );
    }


    // ============================================================
    // 公開API
    // ============================================================

    public float GetCurrentRisk()
    {
        return
            currentRisk;
    }


    public float GetMaximumRisk()
    {
        return
            maximumRisk;
    }


    public float GetNormalizedRisk()
    {
        if (
            maximumRisk <=
            MinimumNonNegativeValue
        )
        {
            return
                MinimumRisk;
        }


        return
            Mathf.Clamp01(
                currentRisk /
                maximumRisk
            );
    }


    public float GetDetectionThreshold()
    {
        return
            detectionThreshold;
    }


    public ExposureRiskState GetCurrentState()
    {
        return
            currentState;
    }


    public bool GetIsPeriscopeExposed()
    {
        return
            isPeriscopeExposed;
    }


    public bool GetIsCommunicationMastExposed()
    {
        return
            isCommunicationMastExposed;
    }


    public bool GetIsAnyExposureSourceActive()
    {
        return
            isPeriscopeExposed ||
            isCommunicationMastExposed;
    }


    public bool GetIsDetectionCooldownActive()
    {
        return
            detectionCooldownRemaining >
            MinimumNonNegativeValue;
    }


    public float GetDetectionCooldownRemaining()
    {
        return
            detectionCooldownRemaining;
    }


    public int GetDetectionCount()
    {
        return
            detectionCount;
    }


    // ============================================================
    // 外部から危険度を設定
    // ============================================================

    /// <summary>
    /// 将来イベント等から危険度を設定する場合に使用。
    /// </summary>
    public void SetRisk(
        float value
    )
    {
        currentRisk =
            Mathf.Clamp(
                value,
                MinimumRisk,
                maximumRisk
            );


        RiskChanged?.Invoke(
            currentRisk
        );


        lastNotifiedRisk =
            currentRisk;
    }


    // ============================================================
    // 外部から危険度加算
    // ============================================================

    /// <summary>
    /// 将来、
    /// 魚雷発射・高速航行・ソナー使用などを
    /// 危険度へ追加する場合に使用できる。
    /// </summary>
    public void AddExternalRisk(
        float amount
    )
    {
        AddRisk(
            amount
        );


        RiskChanged?.Invoke(
            currentRisk
        );


        lastNotifiedRisk =
            currentRisk;
    }


    // ============================================================
    // 危険度リセット
    // ============================================================

    public void ResetRisk()
    {
        currentRisk =
            MinimumRisk;


        detectionCooldownRemaining =
            MinimumNonNegativeValue;


        timeSinceLastExposure =
            0.0f;


        RiskChanged?.Invoke(
            currentRisk
        );


        lastNotifiedRisk =
            currentRisk;


        UpdateState();
    }


    // ============================================================
    // システムON/OFF
    // ============================================================

    public void SetRiskSystemEnabled(
        bool enabled
    )
    {
        riskSystemEnabled =
            enabled;
    }


    public bool GetRiskSystemEnabled()
    {
        return
            riskSystemEnabled;
    }


    // ============================================================
    // Inspector値検証
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    private void ValidateSettings()
    {
        maximumRisk =
            Mathf.Max(
                MinimumNonNegativeValue,
                maximumRisk
            );


        detectionThreshold =
            Mathf.Clamp(
                detectionThreshold,
                MinimumRisk,
                maximumRisk
            );


        initialRisk =
            Mathf.Clamp(
                initialRisk,
                MinimumRisk,
                maximumRisk
            );


        periscopeRiskPerSecond =
            Mathf.Max(
                MinimumNonNegativeValue,
                periscopeRiskPerSecond
            );


        communicationMastRiskPerSecond =
            Mathf.Max(
                MinimumNonNegativeValue,
                communicationMastRiskPerSecond
            );


        simultaneousExposureMultiplier =
            Mathf.Max(
                MinimumNonNegativeValue,
                simultaneousExposureMultiplier
            );


        riskRecoveryPerSecond =
            Mathf.Max(
                MinimumNonNegativeValue,
                riskRecoveryPerSecond
            );


        recoveryStartDelay =
            Mathf.Max(
                MinimumNonNegativeValue,
                recoveryStartDelay
            );


        postDetectionRisk =
            Mathf.Clamp(
                postDetectionRisk,
                MinimumRisk,
                maximumRisk
            );


        detectionCooldownDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                detectionCooldownDuration
            );


        detectionScorePenalty =
            Mathf.Max(
                0,
                detectionScorePenalty
            );
    }
}