using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms.Impl;

public static class DataManager
{
    private static Vector3 submarine_position;  // 潜水艦の座標
    private static float submarine_rotation;    // 潜水艦の向き（y軸）
    private static float submarine_max_speed = 3.0f;   // 潜水艦の最大速度
    private static float enemyShip_max_speed = 3.0f;   // 敵艦の最大速度
    private static float fellowShip_max_speed = 3.0f;  // 味方艦の最大速度

    private static int score;                   // 現在のスコア
    private static List<string> enemy_ships_list = new();   // 敵艦のリスト
    
    


    public static void Initialize()
    {
        SetScore(0);    // スコアを初期化
    }

    /// <summary>
    /// 潜水艦の現在の座標を返す
    /// </summary>
    /// <returns> Vector3 潜水艦の座標 </returns>
    public static Vector3 GetSubmarinePosition()
    {
        return submarine_position;
    }

    /// <summary>
    /// 潜水艦の現在の座標を設定する
    /// </summary>
    /// <param name="position">潜水艦の座標</param>
    /// <returns> bool 成功したかどうか（成功：true，失敗：false） </returns>
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




    /// <summary>
    /// 潜水艦の最大速度を返す
    /// </summary>
    /// <returns>潜水艦の最大速度</returns>
    public static float GetSubmarineMaxSpeed()
    {
        return submarine_max_speed;
    }



    /// <summary>
    /// 敵艦の最大速度を返す
    /// </summary>
    /// <returns>敵艦の最大速度</returns>
    public static float GetEnemyShipMaxSpeed()
    {
        return enemyShip_max_speed;
    }

    /// <summary>
    /// 味方艦の最大速度を返す
    /// </summary>
    /// <returns>味方艦の最大速度</returns>
    public static float GetFellowShipMaxSpeed()
    {
        return fellowShip_max_speed;
    }








    /// <summary>
    /// 現在のスコアを返す
    /// </summary>
    /// <returns> int スコア </returns>
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
