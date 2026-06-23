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

    private static float enemyShip_rotate_radius = 50f;     // 敵艦の回転半径

    private static int score;                   // 現在のスコア
    private static List<string> enemy_ships_list = new();   // 敵艦のリスト
    
    


    public static void Initialize()
    {
        SetScore(0);    // スコアを初期化
        enemy_ships_list.Clear();    // 敵艦のリストを初期化
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
    /// 潜水艦の現在の向きを返す
    /// </summary>
    /// <returns> float 潜水艦の向き（y軸） </returns>
    public static float GetSubmarineRotation()
    {
        return submarine_rotation;
    }

    /// <summary>
    /// 潜水艦の現在の向きを設定する
    /// </summary>
    /// <param name="rotation">潜水艦の向き（y軸）</param>
    /// <returns> bool 成功したかどうか（成功：true，失敗：false） </returns>
    public static bool SetSubmarineRotation(float rotation)
    {
        submarine_rotation = rotation;
        return true;
    }




    /// <summary>
    /// 潜水艦の最大速度を返す
    /// </summary>
    /// <returns>float 潜水艦の最大速度</returns>
    public static float GetSubmarineMaxSpeed()
    {
        return submarine_max_speed;
    }



    /// <summary>
    /// 敵艦の最大速度を返す
    /// </summary>
    /// <returns>float 敵艦の最大速度</returns>
    public static float GetEnemyShipMaxSpeed()
    {
        return enemyShip_max_speed;
    }

    /// <summary>
    /// 味方艦の最大速度を返す
    /// </summary>
    /// <returns>float 味方艦の最大速度</returns>
    public static float GetFellowShipMaxSpeed()
    {
        return fellowShip_max_speed;
    }


    /// <summary>
    /// 敵艦の回転半径を返す
    /// </summary>
    /// <returns>float 敵艦の回転半径</returns>
    public static float GetEnemyShipRotateRadius()
    {
        return enemyShip_rotate_radius;
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



    /**
     * 敵船を追加する
     * @param string enemyShip_name 敵船の名前（例：EnemyShip_1, EnemyShip_21）
     * @return bool 追加成功：true, 追加失敗：false
     */
    public static bool AddEnemyShip(string enemyShip_name)
    {
        // nullチェック
        if (string.IsNullOrWhiteSpace(enemyShip_name)) return false;

        // 形式チェック
        string[] tokens = enemyShip_name.Split("_");
        if (tokens.Length != 2) return false;
        if (tokens[0] != "EnemyShip") return false;

        enemy_ships_list.Add(enemyShip_name);
        return true;
    }

    /**
     * 敵船を削除する
     * @param string enemyShip_name 敵船の名前（例：EnemyShip_1, EnemyShip_21）
     * @return bool 削除成功：true, 削除失敗：false
     */
    public static bool DeleteEnemyShip(string enemyShip_name)
    {
        // nullチェック
        if (string.IsNullOrWhiteSpace(enemyShip_name)) return false;

        return enemy_ships_list.Remove(enemyShip_name);
    }

    /**
     * 敵船の一覧をList<string>型で返す
     * @return List<string> 敵船の一覧
     */
    public static List<string> GetEnemyShipList()
    {
        return enemy_ships_list;
    }

    /**
     * 潜水艦と全ての敵船の距離と方角の情報をListにして返す
     * 
     * ## 情報の入り方
     * [(subm_x - ship_x), (subm_z - ship_z), (distance)]
     * - 潜水艦から敵船への方角：Vector2([0], [1])
     * - 潜水艦から敵船までの距離；[2]
     * 
     * @return List<float[]> 
     */
    public static List<float[]> GetEnemyShipDistanceList()
    {
        List<float[]> EnemyShipDistanceList = new();

        for (int i = 0; i < enemy_ships_list.Count; i++)
        {
            GameObject EnemyShip = GameObject.Find(enemy_ships_list[i]);
            if (EnemyShip == null) continue;

            float[] result = new float[3];

            Vector3 enemyShip_pos = EnemyShip.transform.position;
            float enemyShip_pos_x = enemyShip_pos.x;
            float enemyShip_pos_z = enemyShip_pos.z;

            result[0] = enemyShip_pos_x - submarine_position.x;
            result[1] = enemyShip_pos_z - submarine_position.z;
            result[2] = Mathf.Sqrt((result[0] * result[0]) + (result[1] * result[1]));

            EnemyShipDistanceList.Add(result);
        }

        return EnemyShipDistanceList;
    }
}
