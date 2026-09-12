using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GameTimerUIController : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "残り時間を表示するTextMeshProUGUI")]
    private TMP_Text timerText;


    [SerializeField, Tooltip(
        "通常ゲームのGameManager。" +
        "通常ゲームSceneではこちらを設定する。" +
        "未設定なら自動検索する")]
    private GameManager gameManager;


    [SerializeField, Tooltip(
        "色記憶ゲームのColorMemoryGameManager。" +
        "色記憶ゲームSceneではこちらを設定する。" +
        "未設定なら自動検索する")]
    private ColorMemoryGameManager
        colorMemoryGameManager;


    // ============================================================
    // Display
    // ============================================================

    [Header("Display")]

    [SerializeField, Tooltip(
        "時間の前に表示する文字")]
    private string prefix =
        "残り時間 ";


    [SerializeField, Tooltip(
        "ONなら01:30形式、OFFなら90形式で表示する")]
    private bool useMinutesAndSeconds =
        true;


    [SerializeField, Tooltip(
        "制限時間が0で無制限の場合に表示する文字")]
    private string unlimitedText =
        "∞";


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "参照取得などをConsoleへ表示する")]
    private bool debugLog =
        false;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();

        UpdateTimerText();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        UpdateTimerText();
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<
                    GameManager
                >();
        }


        if (
            colorMemoryGameManager ==
            null
        )
        {
            colorMemoryGameManager =
                FindFirstObjectByType<
                    ColorMemoryGameManager
                >();
        }


        if (
            debugLog &&
            gameManager == null &&
            colorMemoryGameManager == null
        )
        {
            Debug.LogWarning(
                "GameTimerUIController: " +
                "GameManagerまたはColorMemoryGameManagerが見つかりません。"
            );
        }
    }


    // ============================================================
    // UI Update
    // ============================================================

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }


        bool isUnlimited;
        float remainingTime;


        if (gameManager != null)
        {
            isUnlimited =
                gameManager
                    .GetIsUnlimitedTime();


            remainingTime =
                gameManager
                    .GetRemainingTime();
        }
        else if (
            colorMemoryGameManager !=
            null
        )
        {
            isUnlimited =
                colorMemoryGameManager
                    .GetIsUnlimitedTime();


            remainingTime =
                colorMemoryGameManager
                    .GetRemainingTime();
        }
        else
        {
            ResolveReferences();

            return;
        }


        if (isUnlimited)
        {
            timerText.text =
                prefix +
                unlimitedText;

            return;
        }


        timerText.text =
            prefix +
            FormatTime(
                remainingTime
            );
    }


    // ============================================================
    // Format
    // ============================================================

    private string FormatTime(
        float remainingTime
    )
    {
        int totalSeconds =
            Mathf.CeilToInt(
                Mathf.Max(
                    0.0f,
                    remainingTime
                )
            );


        if (!useMinutesAndSeconds)
        {
            return
                totalSeconds
                    .ToString();
        }


        int minutes =
            totalSeconds / 60;


        int seconds =
            totalSeconds % 60;


        return
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00");
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        if (prefix == null)
        {
            prefix =
                string.Empty;
        }


        if (unlimitedText == null)
        {
            unlimitedText =
                string.Empty;
        }
    }
}
