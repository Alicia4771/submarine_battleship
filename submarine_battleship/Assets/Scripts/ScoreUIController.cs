using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreUIController : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "現在のスコアを表示するTextMeshProUGUI")]
    private TMP_Text scoreText;


    // ============================================================
    // Display
    // ============================================================

    [Header("Display")]

    [SerializeField, Tooltip(
        "スコアの前に表示する文字")]
    private string prefix =
        "SCORE ";


    [SerializeField, Tooltip(
        "3桁区切りを使用する。例: 1,250")]
    private bool useThousandsSeparator =
        true;


    // ============================================================
    // Internal
    // ============================================================

    private int previousScore =
        int.MinValue;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        UpdateScoreText(
            forceUpdate: true
        );
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        UpdateScoreText(
            forceUpdate: false
        );
    }


    // ============================================================
    // UI Update
    // ============================================================

    private void UpdateScoreText(
        bool forceUpdate
    )
    {
        if (scoreText == null)
        {
            return;
        }


        int currentScore =
            DataManager.GetScore();


        if (
            !forceUpdate &&
            currentScore ==
            previousScore
        )
        {
            return;
        }


        previousScore =
            currentScore;


        string scoreValue =
            useThousandsSeparator
                ? currentScore.ToString("N0")
                : currentScore.ToString();


        scoreText.text =
            prefix +
            scoreValue;
    }


    // ============================================================
    // Public API
    // ============================================================

    public void RefreshScoreText()
    {
        UpdateScoreText(
            forceUpdate: true
        );
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
    }
}
