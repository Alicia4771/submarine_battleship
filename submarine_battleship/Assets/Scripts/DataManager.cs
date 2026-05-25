using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms.Impl;

public static class DataManager
{
    private static Vector3 submarine_position;  // 潜水艦の座標
    private static float submarine_rotation;    // 潜水艦の向き（y軸）
    
    private static int score;                   // 現在のスコア
    
    
    public static void Initialize()
    {
        SetScore(0);    // スコアを初期化
    }

    /**
     * 潜水艦の現在の座標を返す
     * @return Vector3 潜水艦の座標
     */
    public static Vector3 GetSubmarinePosition()
    {
        return submarine_position;
    }

    /**
     * 潜水艦の現在の座標を設定する
     * @param Vector3 position 潜水艦の座標
     * @return bool 成功したかどうか（成功：true，失敗：false）
     */
    public static bool SetSubmarinePosition(Vector3 position)
    {
        if (position == null)
        {
            Debug.LogError("Invalid position: " + position);
            return false;
        }
        
        submarine_position = position;
        return true;
    }



    /**
     * 現在のスコアを返す
     * @return int スコア
     */
    public static int GetScore()
    {
        return score;
    }

    /**
     * スコアを加算する
     * @param int additional_score 加算するスコア
     * @return bool 成功したかどうか（成功：true，失敗：false）
     */
    public static bool AddScore(int additional_score)
    {
        if (additional_score == null) // 追加スコアがnullの場合はエラー
        {
            Debug.LogError("Invalid additional score: " + additional_score);
            return false;
        }
        
        score += additional_score;
        return true;
    }

    /**
     * スコアを設定する
     * @param int new_score 現在のスコア
     * @return bool 成功したかどうか（成功：true，失敗：false）
     */
    private static bool SetScore(int new_score)
    {
        if (new_score == null) // 新しいスコアがnullの場合はエラー
        {
            Debug.LogError("Invalid new score: " + new_score);
            return false;
        }

        score = new_score;
        return true;
    }
}
