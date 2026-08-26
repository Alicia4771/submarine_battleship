using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CommunicationResultUIController : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float MinimumAlpha =
        0.0f;

    private const float MaximumAlpha =
        1.0f;

    private const float MinimumNonNegativeValue =
        0.0f;


    // ============================================================
    // デフォルト表示時間
    // ============================================================

    private const float DefaultResultDisplayDuration =
        2.0f;

    private const float DefaultFadeInDuration =
        0.15f;

    private const float DefaultFadeOutDuration =
        0.35f;


    // ============================================================
    // 送信中表示
    // ============================================================

    private const string DefaultTransmittingTitle =
        "送信中";

    private const string DefaultTransmittingDetail =
        "司令部へ信号を送信しています\n" +
        "操作せずお待ちください";


    private const string DefaultEvaluatingTitle =
        "照合中";

    private const string DefaultEvaluatingDetail =
        "送信した信号を照合しています\n" +
        "操作せずお待ちください";


    // ============================================================
    // 成功
    // ============================================================

    private const string DefaultSuccessTitle =
        "通信成功";

    private const string DefaultSuccessDetail =
        "信号を正しく送信しました";


    // ============================================================
    // 失敗
    // ============================================================

    private const string DefaultFailureTitle =
        "通信失敗";

    private const string DefaultFailureDetail =
        "信号が一致しませんでした";


    // ============================================================
    // 色
    // ============================================================

    private static readonly Color DefaultTransmittingColor =
        new Color(
            1.0f,
            0.85f,
            0.2f,
            1.0f
        );


    private static readonly Color DefaultEvaluatingColor =
        new Color(
            0.4f,
            0.8f,
            1.0f,
            1.0f
        );


    private static readonly Color DefaultSuccessColor =
        new Color(
            0.25f,
            1.0f,
            0.35f,
            1.0f
        );


    private static readonly Color DefaultFailureColor =
        new Color(
            1.0f,
            0.25f,
            0.25f,
            1.0f
        );


    private static readonly Color DefaultBackgroundColor =
        new Color(
            0.0f,
            0.0f,
            0.0f,
            0.75f
        );


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField, Tooltip(
        "通信ミッションを管理するCommunicationMissionManager。" +
        "未設定の場合は自動検索する")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "通信状態UI全体のCanvasGroup")]
    private CanvasGroup
        resultCanvasGroup;


    [SerializeField, Tooltip(
        "送信中・通信成功・通信失敗などを表示するTextMeshPro")]
    private TMP_Text
        resultText;


    [SerializeField, Tooltip(
        "詳細説明を表示するTextMeshPro")]
    private TMP_Text
        detailText;


    [SerializeField, Tooltip(
        "パネル背景Image。" +
        "不要な場合は未設定でもよい")]
    private Image
        backgroundImage;


    // ============================================================
    // 送信中メッセージ
    // ============================================================

    [Header("Transmitting Messages")]

    [SerializeField]
    private string transmittingTitle =
        DefaultTransmittingTitle;


    [SerializeField, TextArea]
    private string transmittingDetail =
        DefaultTransmittingDetail;


    [SerializeField]
    private string evaluatingTitle =
        DefaultEvaluatingTitle;


    [SerializeField, TextArea]
    private string evaluatingDetail =
        DefaultEvaluatingDetail;


    // ============================================================
    // 結果メッセージ
    // ============================================================

    [Header("Result Messages")]

    [SerializeField]
    private string successTitle =
        DefaultSuccessTitle;


    [SerializeField]
    private string successDetail =
        DefaultSuccessDetail;


    [SerializeField]
    private string failureTitle =
        DefaultFailureTitle;


    [SerializeField]
    private string failureDetail =
        DefaultFailureDetail;


    // ============================================================
    // 色
    // ============================================================

    [Header("Colors")]

    [SerializeField]
    private Color transmittingColor =
        DefaultTransmittingColor;


    [SerializeField]
    private Color evaluatingColor =
        DefaultEvaluatingColor;


    [SerializeField]
    private Color successColor =
        DefaultSuccessColor;


    [SerializeField]
    private Color failureColor =
        DefaultFailureColor;


    [SerializeField]
    private Color backgroundColor =
        DefaultBackgroundColor;


    // ============================================================
    // 時間
    // ============================================================

    [Header("Timing")]

    [SerializeField, Tooltip(
        "通信成功・失敗を表示しておく時間")]
    [Min(MinimumNonNegativeValue)]
    private float resultDisplayDuration =
        DefaultResultDisplayDuration;


    [SerializeField, Tooltip(
        "UIを表示するときのフェード時間。" +
        "0なら即表示")]
    [Min(MinimumNonNegativeValue)]
    private float fadeInDuration =
        DefaultFadeInDuration;


    [SerializeField, Tooltip(
        "結果UIを消すときのフェード時間。" +
        "0なら即非表示")]
    [Min(MinimumNonNegativeValue)]
    private float fadeOutDuration =
        DefaultFadeOutDuration;


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

    private Coroutine
        displayCoroutine;


    private bool resultIsBeingDisplayed =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

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
        // OnEnable時点でManagerが見つからなかった場合にも対応
        ResolveReferences();

        SubscribeEvents();
    }


    // ============================================================
    // OnDisable
    // ============================================================

    private void OnDisable()
    {
        UnsubscribeEvents();


        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );


            displayCoroutine =
                null;
        }
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        // =========================
        // MissionManager
        // =========================

        if (
            communicationMissionManager ==
            null
        )
        {
            communicationMissionManager =
                FindFirstObjectByType<
                    CommunicationMissionManager
                >();
        }


        // =========================
        // CanvasGroup
        // =========================

        if (resultCanvasGroup == null)
        {
            resultCanvasGroup =
                GetComponent<CanvasGroup>();
        }


        // =========================
        // 背景
        // =========================

        if (backgroundImage == null)
        {
            backgroundImage =
                GetComponent<Image>();
        }


        // =========================
        // Error / Warning
        // =========================

        if (
            communicationMissionManager ==
                null
            &&
            debugLog
        )
        {
            Debug.LogWarning(
                "CommunicationResultUIController: " +
                "CommunicationMissionManagerが見つかりません。"
            );
        }


        if (resultCanvasGroup == null)
        {
            Debug.LogError(
                "CommunicationResultUIController: " +
                "CanvasGroupがありません。"
            );
        }
    }


    // ============================================================
    // Event登録
    // ============================================================

    private void SubscribeEvents()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        // =========================
        // 二重登録防止
        // =========================

        communicationMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;


        communicationMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;


        // =========================
        // 登録
        // =========================

        communicationMissionManager
            .MissionEvaluated +=
                HandleMissionEvaluated;


        communicationMissionManager
            .MissionStateChanged +=
                HandleMissionStateChanged;
    }


    // ============================================================
    // Event解除
    // ============================================================

    private void UnsubscribeEvents()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        communicationMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;


        communicationMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;
    }


    // ============================================================
    // UI初期化
    // ============================================================

    private void InitializeUI()
    {
        resultIsBeingDisplayed =
            false;


        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.alpha =
                MinimumAlpha;


            resultCanvasGroup.interactable =
                false;


            resultCanvasGroup.blocksRaycasts =
                false;
        }


        if (backgroundImage != null)
        {
            backgroundImage.color =
                backgroundColor;
        }
    }


    // ============================================================
    // Mission状態変更
    // ============================================================

    private void HandleMissionStateChanged(
        CommunicationMissionManager.MissionState newState
    )
    {
        // 結果表示中は、
        // Success / Failed後のSearchingによって
        // 途中で消されないようにする
        if (resultIsBeingDisplayed)
        {
            return;
        }


        switch (newState)
        {
            // ====================================================
            // 送信中
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Transmitting:

                ShowProcessingState(
                    transmittingTitle,
                    transmittingDetail,
                    transmittingColor
                );

                break;


            // ====================================================
            // 正誤判定中
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Evaluating:

                ShowProcessingState(
                    evaluatingTitle,
                    evaluatingDetail,
                    evaluatingColor
                );

                break;


            // ====================================================
            // それ以外
            // ====================================================

            case CommunicationMissionManager
                .MissionState
                .Searching:

            case CommunicationMissionManager
                .MissionState
                .Memorizing:

            case CommunicationMissionManager
                .MissionState
                .WaitingForSubmerge:

            case CommunicationMissionManager
                .MissionState
                .Inputting:

                HideImmediately();

                break;
        }
    }


    // ============================================================
    // 送信中・判定中表示
    // ============================================================

    private void ShowProcessingState(
        string title,
        string detail,
        Color titleColor
    )
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );


            displayCoroutine =
                null;
        }


        resultIsBeingDisplayed =
            false;


        ApplyContents(
            title,
            detail,
            titleColor
        );


        ShowImmediately();


        if (debugLog)
        {
            Debug.Log(
                "通信状態UI: " +
                title
            );
        }
    }


    // ============================================================
    // 正誤判定結果
    // ============================================================

    private void HandleMissionEvaluated(
        bool wasSuccessful
    )
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );


            displayCoroutine =
                null;
        }


        resultIsBeingDisplayed =
            true;


        if (wasSuccessful)
        {
            ApplyContents(
                successTitle,
                successDetail,
                successColor
            );
        }
        else
        {
            ApplyContents(
                failureTitle,
                failureDetail,
                failureColor
            );
        }


        displayCoroutine =
            StartCoroutine(
                ShowResultRoutine()
            );


        if (debugLog)
        {
            Debug.Log(
                wasSuccessful
                    ? "通信結果UI: 成功"
                    : "通信結果UI: 失敗"
            );
        }
    }


    // ============================================================
    // 表示内容
    // ============================================================

    private void ApplyContents(
        string title,
        string detail,
        Color titleColor
    )
    {
        if (resultText != null)
        {
            resultText.text =
                title;


            resultText.color =
                titleColor;
        }


        if (detailText != null)
        {
            detailText.text =
                detail;
        }


        if (backgroundImage != null)
        {
            backgroundImage.color =
                backgroundColor;
        }
    }


    // ============================================================
    // 即時表示
    // ============================================================

    private void ShowImmediately()
    {
        if (resultCanvasGroup == null)
        {
            return;
        }


        resultCanvasGroup.alpha =
            MaximumAlpha;


        // 今回は通知表示なので
        // UI操作は受け付けない
        resultCanvasGroup.interactable =
            false;


        resultCanvasGroup.blocksRaycasts =
            false;
    }


    // ============================================================
    // 結果表示Coroutine
    // ============================================================

    private IEnumerator ShowResultRoutine()
    {
        if (resultCanvasGroup == null)
        {
            resultIsBeingDisplayed =
                false;


            yield break;
        }


        // ========================================================
        // Fade In
        // ========================================================

        yield return
            FadeCanvasGroup(
                resultCanvasGroup.alpha,
                MaximumAlpha,
                fadeInDuration
            );


        // ========================================================
        // 表示維持
        // ========================================================

        if (
            resultDisplayDuration >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSecondsRealtime(
                    resultDisplayDuration
                );
        }


        // ========================================================
        // Fade Out
        // ========================================================

        yield return
            FadeCanvasGroup(
                resultCanvasGroup.alpha,
                MinimumAlpha,
                fadeOutDuration
            );


        resultCanvasGroup.alpha =
            MinimumAlpha;


        resultIsBeingDisplayed =
            false;


        displayCoroutine =
            null;
    }


    // ============================================================
    // Fade処理
    // ============================================================

    private IEnumerator FadeCanvasGroup(
        float startAlpha,
        float endAlpha,
        float duration
    )
    {
        if (resultCanvasGroup == null)
        {
            yield break;
        }


        // =========================
        // 即時
        // =========================

        if (
            duration <=
            MinimumNonNegativeValue
        )
        {
            resultCanvasGroup.alpha =
                endAlpha;


            yield break;
        }


        // =========================
        // 補間
        // =========================

        float elapsedTime =
            MinimumNonNegativeValue;


        while (
            elapsedTime <
            duration
        )
        {
            elapsedTime +=
                Time.unscaledDeltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );


            resultCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    normalizedTime
                );


            yield return
                null;
        }


        resultCanvasGroup.alpha =
            endAlpha;
    }


    // ============================================================
    // 即時非表示
    // ============================================================

    public void HideImmediately()
    {
        if (resultIsBeingDisplayed)
        {
            return;
        }


        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );


            displayCoroutine =
                null;
        }


        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.alpha =
                MinimumAlpha;
        }
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        resultDisplayDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                resultDisplayDuration
            );


        fadeInDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                fadeInDuration
            );


        fadeOutDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                fadeOutDuration
            );
    }
}