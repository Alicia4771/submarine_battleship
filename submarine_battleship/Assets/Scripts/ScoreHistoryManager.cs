using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ScoreHistoryManager
{
    // ============================================================
    // Constants
    // ============================================================

    private const string SaveFileName =
        "score_history.json";


    private const int DefaultMaximumStoredEntries =
        100;


    // ============================================================
    // Data Classes
    // ============================================================

    [Serializable]
    public class ScoreRecord
    {
        public int score;

        public string playedAt;

        public string gameMode;


        public ScoreRecord(
            int scoreValue,
            string playedAtValue,
            string gameModeValue
        )
        {
            score =
                scoreValue;


            playedAt =
                playedAtValue;


            gameMode =
                gameModeValue;
        }
    }


    [Serializable]
    private class ScoreHistoryData
    {
        public List<ScoreRecord> records =
            new List<ScoreRecord>();
    }


    // ============================================================
    // Save Path
    // ============================================================

    public static string GetSaveFilePath()
    {
        return
            Path.Combine(
                Application.persistentDataPath,
                SaveFileName
            );
    }


    // ============================================================
    // Save Score
    // ============================================================

    public static void SaveScore(
        int score,
        string gameMode = "",
        int maximumStoredEntries =
            DefaultMaximumStoredEntries
    )
    {
        ScoreHistoryData historyData =
            LoadHistoryData();


        if (historyData.records == null)
        {
            historyData.records =
                new List<ScoreRecord>();
        }


        ScoreRecord newRecord =
            new ScoreRecord(
                score,
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss"
                ),
                gameMode ?? string.Empty
            );


        historyData.records.Add(
            newRecord
        );


        SortByScoreDescending(
            historyData.records
        );


        int validatedMaximum =
            Mathf.Max(
                1,
                maximumStoredEntries
            );


        if (
            historyData.records.Count >
            validatedMaximum
        )
        {
            historyData.records.RemoveRange(
                validatedMaximum,
                historyData.records.Count -
                validatedMaximum
            );
        }


        WriteHistoryData(
            historyData
        );
    }


    // ============================================================
    // Load Ranking
    // ============================================================

    public static List<ScoreRecord> LoadRanking(
        string gameModeFilter = ""
    )
    {
        ScoreHistoryData historyData =
            LoadHistoryData();


        List<ScoreRecord> ranking =
            new List<ScoreRecord>();


        if (historyData.records == null)
        {
            return ranking;
        }


        bool useFilter =
            !string.IsNullOrWhiteSpace(
                gameModeFilter
            );


        for (
            int index = 0;
            index < historyData.records.Count;
            index++
        )
        {
            ScoreRecord record =
                historyData.records[index];


            if (record == null)
            {
                continue;
            }


            if (
                useFilter &&
                record.gameMode !=
                gameModeFilter
            )
            {
                continue;
            }


            ranking.Add(
                record
            );
        }


        SortByScoreDescending(
            ranking
        );


        return ranking;
    }


    // ============================================================
    // Clear
    // ============================================================

    public static bool ClearHistory()
    {
        string savePath =
            GetSaveFilePath();


        try
        {
            if (
                File.Exists(
                    savePath
                )
            )
            {
                File.Delete(
                    savePath
                );
            }


            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ScoreHistoryManager: " +
                "スコア履歴の削除に失敗しました。\n" +
                exception
            );


            return false;
        }
    }


    // ============================================================
    // Load Internal
    // ============================================================

    private static ScoreHistoryData
        LoadHistoryData()
    {
        string savePath =
            GetSaveFilePath();


        if (
            !File.Exists(
                savePath
            )
        )
        {
            return
                new ScoreHistoryData();
        }


        try
        {
            string json =
                File.ReadAllText(
                    savePath
                );


            if (
                string.IsNullOrWhiteSpace(
                    json
                )
            )
            {
                return
                    new ScoreHistoryData();
            }


            ScoreHistoryData data =
                JsonUtility.FromJson<
                    ScoreHistoryData
                >(
                    json
                );


            if (data == null)
            {
                return
                    new ScoreHistoryData();
            }


            if (data.records == null)
            {
                data.records =
                    new List<ScoreRecord>();
            }


            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ScoreHistoryManager: " +
                "スコア履歴の読み込みに失敗しました。\n" +
                exception
            );


            return
                new ScoreHistoryData();
        }
    }


    // ============================================================
    // Write Internal
    // ============================================================

    private static void WriteHistoryData(
        ScoreHistoryData historyData
    )
    {
        string savePath =
            GetSaveFilePath();


        try
        {
            string directory =
                Path.GetDirectoryName(
                    savePath
                );


            if (
                !string.IsNullOrEmpty(
                    directory
                )
            )
            {
                Directory.CreateDirectory(
                    directory
                );
            }


            string json =
                JsonUtility.ToJson(
                    historyData,
                    true
                );


            File.WriteAllText(
                savePath,
                json
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ScoreHistoryManager: " +
                "スコア履歴の保存に失敗しました。\n" +
                exception
            );
        }
    }


    // ============================================================
    // Sort
    // ============================================================

    private static void SortByScoreDescending(
        List<ScoreRecord> records
    )
    {
        records.Sort(
            (left, right) =>
            {
                if (
                    left == null &&
                    right == null
                )
                {
                    return 0;
                }


                if (left == null)
                {
                    return 1;
                }


                if (right == null)
                {
                    return -1;
                }


                int scoreComparison =
                    right.score.CompareTo(
                        left.score
                    );


                if (
                    scoreComparison !=
                    0
                )
                {
                    return scoreComparison;
                }


                return
                    string.Compare(
                        right.playedAt,
                        left.playedAt,
                        StringComparison.Ordinal
                    );
            }
        );
    }
}
