using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ColorMemoryResultUIController : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField]
    private ColorMemoryMissionManager
        colorMemoryMissionManager;


    [SerializeField]
    private CanvasGroup
        resultCanvasGroup;


    [SerializeField]
    private Image
        backgroundImage;


    [SerializeField]
    private TMP_Text
        resultText;


    [SerializeField]
    private TMP_Text
        detailText;


    // ============================================================
    // Timing
    // ============================================================

    [Header("Timing")]

    [SerializeField]
    private float resultDisplayDuration =
        1.5f;


    [SerializeField]
    private float fadeOutDuration =
        0.2f;


    // ============================================================
    // Internal
    // ============================================================

    private Coroutine displayCoroutine;


    private bool displayingResult =
        false;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        HideImmediately();
    }


    // ============================================================
    // Enable
    // ============================================================

    private void OnEnable()
    {
        ResolveReferences();

        SubscribeEvents();
    }


    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        UnsubscribeEvents();
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<
                    ColorMemoryMissionManager
                >();
        }
    }


    // ============================================================
    // Events
    // ============================================================

    private void SubscribeEvents()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        colorMemoryMissionManager
            .MissionStateChanged -=
                HandleStateChanged;


        colorMemoryMissionManager
            .MissionStateChanged +=
                HandleStateChanged;


        colorMemoryMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;


        colorMemoryMissionManager
            .MissionEvaluated +=
                HandleMissionEvaluated;
    }


    private void UnsubscribeEvents()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        colorMemoryMissionManager
            .MissionStateChanged -=
                HandleStateChanged;


        colorMemoryMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;
    }


    // ============================================================
    // State
    // ============================================================

    private void HandleStateChanged(
        ColorMemoryMissionManager
            .MissionState state
    )
    {
        if (displayingResult)
        {
            return;
        }


        switch (state)
        {
            case ColorMemoryMissionManager
                .MissionState
                .Transmitting:

                Show(
                    "送信中",
                    "色の順番を送信しています",
                    Color.white
                );

                break;


            case ColorMemoryMissionManager
                .MissionState
                .Evaluating:

                Show(
                    "照合中",
                    "敵艦の色信号と照合しています",
                    Color.white
                );

                break;


            default:

                HideImmediately();

                break;
        }
    }


    // ============================================================
    // Result
    // ============================================================

    private void HandleMissionEvaluated(
        bool success
    )
    {
        displayingResult =
            true;


        if (success)
        {
            Show(
                "通信成功",
                "色の順番が一致しました",
                Color.green
            );
        }
        else
        {
            Show(
                "通信失敗",
                "色の順番が一致しませんでした",
                Color.red
            );
        }


        if (displayCoroutine != null)
        {
            StopCoroutine(
                displayCoroutine
            );
        }


        displayCoroutine =
            StartCoroutine(
                HideAfterDelay()
            );
    }


    // ============================================================
    // Show
    // ============================================================

    private void Show(
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


        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.alpha =
                1.0f;
        }


        if (backgroundImage != null)
        {
            backgroundImage.color =
                new Color(
                    0.0f,
                    0.0f,
                    0.0f,
                    0.75f
                );
        }
    }


    // ============================================================
    // Hide
    // ============================================================

    private IEnumerator HideAfterDelay()
    {
        yield return
            new WaitForSecondsRealtime(
                resultDisplayDuration
            );


        float startAlpha =
            resultCanvasGroup != null
                ? resultCanvasGroup.alpha
                : 0.0f;


        float elapsed =
            0.0f;


        while (
            resultCanvasGroup != null &&
            elapsed <
            fadeOutDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;


            resultCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0.0f,
                    elapsed /
                    fadeOutDuration
                );


            yield return
                null;
        }


        displayingResult =
            false;


        displayCoroutine =
            null;


        HideImmediately();
    }


    public void HideImmediately()
    {
        if (
            resultCanvasGroup !=
            null
        )
        {
            resultCanvasGroup.alpha =
                0.0f;


            resultCanvasGroup.interactable =
                false;


            resultCanvasGroup.blocksRaycasts =
                false;
        }
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        resultDisplayDuration =
            Mathf.Max(
                0.0f,
                resultDisplayDuration
            );


        fadeOutDuration =
            Mathf.Max(
                0.0f,
                fadeOutDuration
            );
    }
}