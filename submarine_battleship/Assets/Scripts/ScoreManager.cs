using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class ScoreManager : MonoBehaviour
{
    // ============================================================
    // Constants
    // ============================================================

    private const float MinimumNonNegativeValue = 0.0f;
    private const float MinimumPositiveValue = 0.001f;

    private const float DefaultScoreInterval = 1.0f;
    private const float DefaultSubmergedScorePerSecond = 3.0f;
    private const float DefaultPeriscopeScorePerSecond = 12.0f;
    private const float DefaultCommunicationMastScorePerSecond = 3.0f;

    private const float FloatingPointTolerance = 0.0001f;


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "潜望鏡・通信マストの露出状態を取得するExposureRiskManager。" +
        "未設定の場合はシーン内から自動検索する")]
    private ExposureRiskManager exposureRiskManager;


    // ============================================================
    // Time Score
    // ============================================================

    [Header("Time Score")]

    [SerializeField, Tooltip(
        "時間によるスコア加算を有効にする")]
    private bool timeScoreEnabled = true;


    [SerializeField, Tooltip(
        "スコアをDataManagerへ反映する間隔。" +
        "1.0なら約1秒ごとに反映する")]
    [Min(MinimumPositiveValue)]
    private float scoreInterval = DefaultScoreInterval;


    [SerializeField, Tooltip(
        "潜望鏡も通信マストも露出していない安全状態で、" +
        "1秒あたりに獲得するスコア")]
    [Min(MinimumNonNegativeValue)]
    private float submergedScorePerSecond =
        DefaultSubmergedScorePerSecond;


    [SerializeField, Tooltip(
        "潜望鏡が海面上に露出している間、" +
        "1秒あたりに獲得するスコア")]
    [Min(MinimumNonNegativeValue)]
    private float periscopeScorePerSecond =
        DefaultPeriscopeScorePerSecond;


    [SerializeField, Tooltip(
        "潜望鏡は露出しておらず、通信マストが露出している間、" +
        "1秒あたりに獲得するスコア")]
    [Min(MinimumNonNegativeValue)]
    private float communicationMastScorePerSecond =
        DefaultCommunicationMastScorePerSecond;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "時間スコアを反映するたびにConsoleへ表示する")]
    private bool debugLog = false;


    [SerializeField, Tooltip(
        "Inspector確認用。現在判定されている1秒あたりのスコア")]
    private float currentScorePerSecond =
        DefaultSubmergedScorePerSecond;


    [SerializeField, Tooltip(
        "Inspector確認用。次回反映まで内部に保持している小数スコア")]
    private float pendingScore = 0.0f;


    // ============================================================
    // Internal
    // ============================================================

    private float intervalTimer = 0.0f;


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
        ResolveReferences();

        intervalTimer = 0.0f;
        pendingScore = 0.0f;

        currentScorePerSecond =
            GetCurrentScorePerSecond();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (!timeScoreEnabled)
        {
            currentScorePerSecond = 0.0f;
            return;
        }


        float deltaTime = Time.deltaTime;


        if (deltaTime <= MinimumNonNegativeValue)
        {
            return;
        }


        currentScorePerSecond =
            GetCurrentScorePerSecond();


        AccumulateTimeScore(
            deltaTime,
            currentScorePerSecond
        );
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (exposureRiskManager == null)
        {
            exposureRiskManager =
                FindFirstObjectByType<
                    ExposureRiskManager
                >();
        }
    }


    // ============================================================
    // Score accumulation
    // ============================================================

    private void AccumulateTimeScore(
        float deltaTime,
        float scorePerSecond
    )
    {
        float remainingTime = deltaTime;


        while (remainingTime > MinimumNonNegativeValue)
        {
            float timeUntilNextApply =
                scoreInterval -
                intervalTimer;


            float stepTime =
                Mathf.Min(
                    remainingTime,
                    timeUntilNextApply
                );


            pendingScore +=
                scorePerSecond *
                stepTime;


            intervalTimer += stepTime;

            remainingTime -= stepTime;


            if (
                intervalTimer +
                FloatingPointTolerance <
                scoreInterval
            )
            {
                continue;
            }


            ApplyPendingScore();

            intervalTimer = 0.0f;
        }
    }


    // ============================================================
    // Apply
    // ============================================================

    private void ApplyPendingScore()
    {
        int scoreToAdd =
            Mathf.FloorToInt(
                pendingScore +
                FloatingPointTolerance
            );


        if (scoreToAdd <= 0)
        {
            return;
        }


        pendingScore -= scoreToAdd;


        if (pendingScore < MinimumNonNegativeValue)
        {
            pendingScore = MinimumNonNegativeValue;
        }


        DataManager.AddScore(
            scoreToAdd
        );


        if (debugLog)
        {
            Debug.Log(
                "時間スコア +" +
                scoreToAdd +
                " / Rate=" +
                currentScorePerSecond.ToString("0.##") +
                "/秒 / Total=" +
                DataManager.GetScore()
            );
        }
    }


    // ============================================================
    // Current state score
    // ============================================================

    private float GetCurrentScorePerSecond()
    {
        bool periscopeExposed =
            GetIsPeriscopeExposed();


        bool communicationMastExposed =
            GetIsCommunicationMastExposed();


        // 潜望鏡が露出している場合を最優先。
        // 通常は潜望鏡と通信マストを同時に露出しない想定。
        if (periscopeExposed)
        {
            return periscopeScorePerSecond;
        }


        if (communicationMastExposed)
        {
            return communicationMastScorePerSecond;
        }


        return submergedScorePerSecond;
    }


    // ============================================================
    // Exposure state
    // ============================================================

    private bool GetIsPeriscopeExposed()
    {
        if (exposureRiskManager != null)
        {
            return
                exposureRiskManager
                    .GetIsPeriscopeExposed();
        }


        // ExposureRiskManagerがない場合の予備処理
        return
            DataManager
                .GetIsPeriscopeAboveSurface();
    }


    private bool GetIsCommunicationMastExposed()
    {
        if (exposureRiskManager != null)
        {
            return
                exposureRiskManager
                    .GetIsCommunicationMastExposed();
        }


        return false;
    }


    // ============================================================
    // Public API
    // ============================================================

    public bool GetTimeScoreEnabled()
    {
        return timeScoreEnabled;
    }


    public void SetTimeScoreEnabled(
        bool enabled
    )
    {
        timeScoreEnabled = enabled;


        if (!timeScoreEnabled)
        {
            currentScorePerSecond = 0.0f;
        }
    }


    public float GetCurrentScorePerSecondForDisplay()
    {
        return currentScorePerSecond;
    }


    public float GetPendingScore()
    {
        return pendingScore;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    private void ValidateSettings()
    {
        scoreInterval =
            Mathf.Max(
                MinimumPositiveValue,
                scoreInterval
            );


        submergedScorePerSecond =
            Mathf.Max(
                MinimumNonNegativeValue,
                submergedScorePerSecond
            );


        periscopeScorePerSecond =
            Mathf.Max(
                MinimumNonNegativeValue,
                periscopeScorePerSecond
            );


        communicationMastScorePerSecond =
            Mathf.Max(
                MinimumNonNegativeValue,
                communicationMastScorePerSecond
            );
    }
}
