using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ResultRankingUIController : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "今回のスコアを表示するTextMeshProUGUI")]
    private TMP_Text currentScoreText;


    [SerializeField, Tooltip(
        "ランキングを表示するTextMeshProUGUI")]
    private TMP_Text rankingText;


    // ============================================================
    // Save
    // ============================================================

    [Header("Save")]

    [SerializeField, Tooltip(
        "ResultSceneに入ったとき、現在のDataManagerスコアを保存する")]
    private bool saveCurrentScoreOnStart =
        true;


    [SerializeField, Tooltip(
        "保存するゲームモード名。" +
        "例: Normal / ColorMemory。" +
        "全モード共通ランキングなら空欄でもよい")]
    private string gameModeName =
        "";


    [SerializeField, Min(1), Tooltip(
        "保存ファイルに保持する最大スコア件数")]
    private int maximumStoredEntries =
        100;


    // ============================================================
    // Ranking Display
    // ============================================================

    [Header("Ranking Display")]

    [SerializeField, Min(1), Tooltip(
        "画面に表示するランキング件数")]
    private int maximumDisplayEntries =
        10;


    [SerializeField, Tooltip(
        "ONならgameModeNameと同じモードだけをランキング表示する。" +
        "OFFなら全モード共通ランキング")]
    private bool filterRankingByGameMode =
        false;


    [SerializeField, Tooltip(
        "今回のスコア表示の前に付ける文字")]
    private string currentScorePrefix =
        "今回のスコア：";


    [SerializeField, Tooltip(
        "ランキングが1件もない場合に表示する文字")]
    private string emptyRankingText =
        "まだスコアがありません";


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        int currentScore =
            DataManager.GetScore();


        if (
            saveCurrentScoreOnStart
        )
        {
            ScoreHistoryManager.SaveScore(
                currentScore,
                gameModeName,
                maximumStoredEntries
            );


            if (debugLog)
            {
                Debug.Log(
                    "今回のスコアを保存しました: " +
                    currentScore +
                    "\n保存先: " +
                    ScoreHistoryManager
                        .GetSaveFilePath()
                );
            }
        }


        UpdateCurrentScoreText(
            currentScore
        );


        UpdateRankingText();
    }


    // ============================================================
    // Current Score
    // ============================================================

    private void UpdateCurrentScoreText(
        int currentScore
    )
    {
        if (
            currentScoreText ==
            null
        )
        {
            return;
        }


        currentScoreText.text =
            currentScorePrefix +
            currentScore.ToString(
                "N0"
            );
    }


    // ============================================================
    // Ranking
    // ============================================================

    public void UpdateRankingText()
    {
        if (
            rankingText ==
            null
        )
        {
            return;
        }


        string filter =
            filterRankingByGameMode
                ? gameModeName
                : string.Empty;


        List<
            ScoreHistoryManager.ScoreRecord
        >
            ranking =
                ScoreHistoryManager
                    .LoadRanking(
                        filter
                    );


        if (
            ranking == null ||
            ranking.Count <= 0
        )
        {
            rankingText.text =
                emptyRankingText;

            return;
        }


        int displayCount =
            Mathf.Min(
                maximumDisplayEntries,
                ranking.Count
            );


        StringBuilder builder =
            new StringBuilder();


        for (
            int index = 0;
            index < displayCount;
            index++
        )
        {
            ScoreHistoryManager.ScoreRecord
                record =
                    ranking[index];


            int rank =
                index + 1;


            builder.Append(
                rank
            );


            builder.Append(
                "位　"
            );


            builder.Append(
                record.score.ToString(
                    "N0"
                )
            );


            if (
                !string.IsNullOrWhiteSpace(
                    record.gameMode
                )
            )
            {
                builder.Append(
                    "　"
                );


                builder.Append(
                    record.gameMode
                );
            }


            if (
                index <
                displayCount - 1
            )
            {
                builder.AppendLine();
            }
        }


        rankingText.text =
            builder.ToString();
    }


    // ============================================================
    // Clear
    // ============================================================

    public void ClearRanking()
    {
        bool succeeded =
            ScoreHistoryManager
                .ClearHistory();


        if (!succeeded)
        {
            return;
        }


        UpdateRankingText();


        if (debugLog)
        {
            Debug.Log(
                "スコア履歴を削除しました。"
            );
        }
    }


    // ============================================================
    // Debug
    // ============================================================

    [ContextMenu(
        "Show Score Save Path"
    )]
    private void ShowScoreSavePath()
    {
        Debug.Log(
            "Score save path:\n" +
            ScoreHistoryManager
                .GetSaveFilePath()
        );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        maximumStoredEntries =
            Mathf.Max(
                1,
                maximumStoredEntries
            );


        maximumDisplayEntries =
            Mathf.Max(
                1,
                maximumDisplayEntries
            );


        if (
            gameModeName ==
            null
        )
        {
            gameModeName =
                string.Empty;
        }


        if (
            currentScorePrefix ==
            null
        )
        {
            currentScorePrefix =
                string.Empty;
        }


        if (
            emptyRankingText ==
            null
        )
        {
            emptyRankingText =
                string.Empty;
        }
    }
}
