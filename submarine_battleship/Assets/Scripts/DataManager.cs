using UnityEngine;
using System.Collections.Generic;

public static class DataManager
{
    // =========================
    // 潜水艦
    // =========================

    private static Vector3 submarine_position;        // 潜水艦の座標
    private static float submarine_rotation;          // 潜水艦の向き（y軸）
    private static float submarine_max_speed = 3.0f;  // 潜水艦の最大速度


    // =========================
    // 敵艦・味方艦
    // =========================

    private static float enemyShip_max_speed = 3.0f;   // 敵艦の最大速度
    private static float fellowShip_max_speed = 3.0f;  // 味方艦の最大速度

    private static float enemyShip_rotate_radius = 50f; // 敵艦の回転半径


    // =========================
    // センサー
    // =========================

    private static float sensor_yaw = 0f;
    private static float sensor_speed = 0f;

    private static int sensor_button1 = 0;
    private static int sensor_button2 = 0;
    private static int sensor_button3 = 0;
    private static int sensor_button4 = 0;
    private static int sensor_button5 = 0;
    private static int sensor_button6 = 0;


    // =========================
    // スコア・敵艦リスト
    // =========================

    private static int score;

    private static List<string> enemy_ships_list = new();


    // =========================
    // その他設定
    // =========================

    private static bool changeSceneConfirmation = true;

    private static bool sonar_panel_underwater_canopen = false;


    // ============================================================
    // 初期化
    // ============================================================

    public static void Initialize()
    {
        // スコアを初期化
        SetScore(0);

        // 敵艦リストを初期化
        enemy_ships_list.Clear();


        // センサー値を初期化
        SetSensorYaw(0f);
        SetSensorSpeed(0f);

        SetSensorButton1(0);
        SetSensorButton2(0);
        SetSensorButton3(0);
        SetSensorButton4(0);
        SetSensorButton5(0);
        SetSensorButton6(0);
    }


    // ============================================================
    // 潜水艦
    // ============================================================

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
            Debug.LogError(
                "Invalid position: " + position
            );

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


    // ============================================================
    // 敵艦・味方艦
    // ============================================================

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


    // ============================================================
    // スコア
    // ============================================================

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
        if (additional_score == null)
        {
            Debug.LogError(
                "Invalid additional score: " + additional_score
            );

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
        if (new_score == null)
        {
            Debug.LogError(
                "Invalid new score: " + new_score
            );

            return false;
        }

        score = new_score;

        return true;
    }


    // ============================================================
    // 敵艦リスト
    // ============================================================

    /**
     * 敵船を追加する
     * @param string enemyShip_name 敵船の名前（例：EnemyShip_1, EnemyShip_21）
     * @return bool 追加成功：true, 追加失敗：false
     */
    public static bool AddEnemyShip(string enemyShip_name)
    {
        // nullチェック
        if (string.IsNullOrWhiteSpace(enemyShip_name))
        {
            return false;
        }


        // 形式チェック
        string[] tokens = enemyShip_name.Split("_");


        if (tokens.Length != 2)
        {
            return false;
        }


        if (tokens[0] != "EnemyShip")
        {
            return false;
        }


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
        if (string.IsNullOrWhiteSpace(enemyShip_name))
        {
            return false;
        }


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


        for (
            int i = 0;
            i < enemy_ships_list.Count;
            i++
        )
        {
            GameObject EnemyShip =
                GameObject.Find(
                    enemy_ships_list[i]
                );


            if (EnemyShip == null)
            {
                continue;
            }


            float[] result = new float[3];


            Vector3 enemyShip_pos =
                EnemyShip.transform.position;


            float enemyShip_pos_x =
                enemyShip_pos.x;

            float enemyShip_pos_z =
                enemyShip_pos.z;


            result[0] =
                enemyShip_pos_x -
                submarine_position.x;

            result[1] =
                enemyShip_pos_z -
                submarine_position.z;

            result[2] =
                Mathf.Sqrt(
                    (result[0] * result[0]) +
                    (result[1] * result[1])
                );


            EnemyShipDistanceList.Add(result);
        }


        return EnemyShipDistanceList;
    }


    // ============================================================
    // センサー
    // ============================================================

    // =========================
    // Yaw
    // =========================

    /// <summary>
    /// センサーのyaw角度を取得する
    /// </summary>
    public static float GetSensorYaw()
    {
        return sensor_yaw;
    }


    /// <summary>
    /// センサーのyaw角度を設定する
    /// </summary>
    public static bool SetSensorYaw(float yaw)
    {
        sensor_yaw = yaw;

        return true;
    }


    // =========================
    // Speed
    // =========================

    /// <summary>
    /// センサーのspeedを取得する
    /// </summary>
    public static float GetSensorSpeed()
    {
        return sensor_speed;
    }


    /// <summary>
    /// センサーのspeedを設定する
    /// </summary>
    public static bool SetSensorSpeed(float speed)
    {
        sensor_speed = speed;

        return true;
    }


    // =========================
    // Button1
    // =========================

    public static int GetSensorButton1()
    {
        return sensor_button1;
    }


    public static bool SetSensorButton1(int value)
    {
        if (
            value != 0 &&
            value != 1
        )
        {
            Debug.LogError(
                "Invalid Button1 value: " + value
            );

            return false;
        }


        sensor_button1 = value;

        return true;
    }


    // =========================
    // Button2
    // =========================

    public static int GetSensorButton2()
    {
        return sensor_button2;
    }


    public static bool SetSensorButton2(int value)
    {
        if (
            value != 0 &&
            value != 1
        )
        {
            Debug.LogError(
                "Invalid Button2 value: " + value
            );

            return false;
        }


        sensor_button2 = value;

        return true;
    }


    // =========================
    // Button3
    // =========================

    public static int GetSensorButton3()
    {
        return sensor_button3;
    }


    public static bool SetSensorButton3(int value)
    {
        if (
            value != 0 &&
            value != 1
        )
        {
            Debug.LogError(
                "Invalid Button3 value: " + value
            );

            return false;
        }


        sensor_button3 = value;

        return true;
    }


    // =========================
    // Button4
    // =========================

    public static int GetSensorButton4()
    {
        return sensor_button4;
    }


    public static bool SetSensorButton4(int value)
    {
        if (
            value != 0 &&
            value != 1
        )
        {
            Debug.LogError(
                "Invalid Button4 value: " + value
            );

            return false;
        }


        sensor_button4 = value;

        return true;
    }


    // =========================
    // Button5
    // =========================

    public static int GetSensorButton5()
    {
        return sensor_button5;
    }


    public static bool SetSensorButton5(int value)
    {
        if (
            value != 0 &&
            value != 1
        )
        {
            Debug.LogError(
                "Invalid Button5 value: " + value
            );

            return false;
        }


        sensor_button5 = value;

        return true;
    }


    // =========================
    // Button6
    // =========================

    public static int GetSensorButton6()
    {
        return sensor_button6;
    }


    public static bool SetSensorButton6(int value)
    {
        if (
            value != 0 &&
            value != 1
        )
        {
            Debug.LogError(
                "Invalid Button6 value: " + value
            );

            return false;
        }


        sensor_button6 = value;

        return true;
    }


    // ============================================================
    // シーン変更確認
    // ============================================================

    /// <summary>
    /// シーンを変更する際の確認ダイアログを表示するかどうかを返す
    /// </summary>
    /// <returns>bool</returns>
    public static bool GetChangeSceneConfirmation()
    {
        return changeSceneConfirmation;
    }


    /// <summary>
    /// シーンを変更する際の確認ダイアログを表示するかどうかを設定する
    /// </summary>
    /// <param name="isEnabled"></param>
    public static void SetChangeSceneConfirmation(
        bool isEnabled
    )
    {
        changeSceneConfirmation = isEnabled;
    }


    // ============================================================
    // ソナー
    // ============================================================

    public static bool GetSonarPanelUnderwaterCanOpen()
    {
        return sonar_panel_underwater_canopen;
    }


    public static void SetSonarPanelUnderwaterCanOpen(
        bool canOpen
    )
    {
        sonar_panel_underwater_canopen = canOpen;
    }
}