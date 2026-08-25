using UnityEngine;
using System.Collections.Generic;

public static class DataManager
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultSubmarineMaxSpeed = 3.0f;

    private const float DefaultEnemyShipMaxSpeed = 3.0f;
    private const float DefaultFellowShipMaxSpeed = 3.0f;
    private const float DefaultEnemyShipRotateRadius = 50.0f;

    private const float MinimumNonNegativeValue = 0.0f;

    private const float FullRotationDegrees = 360.0f;

    private const int ButtonReleased = 0;
    private const int ButtonPressed = 1;

    private const string EnemyShipNamePrefix = "EnemyShip";
    private const int EnemyShipNameTokenCount = 2;

    private const int DistanceDataDirectionXIndex = 0;
    private const int DistanceDataDirectionZIndex = 1;
    private const int DistanceDataDistanceIndex = 2;
    private const int DistanceDataLength = 3;


    // ============================================================
    // 潜水艦
    // ============================================================

    private static Vector3 submarine_position =
        Vector3.zero;

    private static float submarine_rotation =
        0.0f;

    private static float submarine_max_speed =
        DefaultSubmarineMaxSpeed;


    // ============================================================
    // 潜望鏡
    // ============================================================

    // 潜望鏡の実際の視点位置
    private static Vector3 periscope_position =
        Vector3.zero;


    // 潜望鏡が現在向いているWorld Yaw
    private static float periscope_rotation =
        0.0f;


    // PeriscopeRootのLocal Y
    private static float periscope_local_height =
        0.0f;


    // 潜望鏡の視点が海面より上にあるか
    private static bool periscope_is_above_surface =
        false;


    // 潜望鏡が上限位置まで上がっているか
    private static bool periscope_is_fully_raised =
        false;


    // 潜望鏡が下限位置まで下がっているか
    private static bool periscope_is_fully_lowered =
        false;


    // ============================================================
    // 敵艦・味方艦
    // ============================================================

    private static float enemyShip_max_speed =
        DefaultEnemyShipMaxSpeed;


    private static float fellowShip_max_speed =
        DefaultFellowShipMaxSpeed;


    private static float enemyShip_rotate_radius =
        DefaultEnemyShipRotateRadius;


    // ============================================================
    // センサー
    // ============================================================

    private static float sensor_yaw =
        0.0f;


    private static float sensor_speed =
        0.0f;


    private static int sensor_button1 =
        ButtonReleased;

    private static int sensor_button2 =
        ButtonReleased;

    private static int sensor_button3 =
        ButtonReleased;

    private static int sensor_button4 =
        ButtonReleased;

    private static int sensor_button5 =
        ButtonReleased;

    private static int sensor_button6 =
        ButtonReleased;


    // ============================================================
    // スコア・敵艦リスト
    // ============================================================

    private static int score =
        0;


    private static readonly List<string>
        enemy_ships_list =
            new();


    // ============================================================
    // その他設定
    // ============================================================

    private static bool changeSceneConfirmation =
        true;


    private static bool sonar_panel_underwater_canopen =
        false;


    // ============================================================
    // 初期化
    // ============================================================

    public static void Initialize()
    {
        // =========================
        // 潜水艦
        // =========================

        submarine_position =
            Vector3.zero;

        submarine_rotation =
            0.0f;


        // =========================
        // 潜望鏡
        // =========================

        periscope_position =
            Vector3.zero;

        periscope_rotation =
            0.0f;

        periscope_local_height =
            0.0f;

        periscope_is_above_surface =
            false;

        periscope_is_fully_raised =
            false;

        periscope_is_fully_lowered =
            false;


        // =========================
        // スコア
        // =========================

        SetScore(0);


        // =========================
        // 敵艦一覧
        // =========================

        enemy_ships_list.Clear();


        // =========================
        // センサー
        // =========================

        SetSensorYaw(
            0.0f
        );

        SetSensorSpeed(
            0.0f
        );


        SetSensorButton1(
            ButtonReleased
        );

        SetSensorButton2(
            ButtonReleased
        );

        SetSensorButton3(
            ButtonReleased
        );

        SetSensorButton4(
            ButtonReleased
        );

        SetSensorButton5(
            ButtonReleased
        );

        SetSensorButton6(
            ButtonReleased
        );
    }


    // ============================================================
    // 潜水艦
    // ============================================================

    public static Vector3 GetSubmarinePosition()
    {
        return
            submarine_position;
    }


    public static bool SetSubmarinePosition(
        Vector3 position
    )
    {
        if (!IsFinite(position))
        {
            Debug.LogError(
                "Invalid submarine position: " +
                position
            );

            return false;
        }


        submarine_position =
            position;


        return true;
    }


    public static float GetSubmarineRotation()
    {
        return
            submarine_rotation;
    }


    public static bool SetSubmarineRotation(
        float rotation
    )
    {
        if (!IsFinite(rotation))
        {
            Debug.LogError(
                "Invalid submarine rotation: " +
                rotation
            );

            return false;
        }


        submarine_rotation =
            NormalizeAngle(
                rotation
            );


        return true;
    }


    public static float GetSubmarineMaxSpeed()
    {
        return
            submarine_max_speed;
    }


    public static bool SetSubmarineMaxSpeed(
        float maxSpeed
    )
    {
        if (
            !IsFinite(maxSpeed) ||
            maxSpeed <
            MinimumNonNegativeValue
        )
        {
            return false;
        }


        submarine_max_speed =
            maxSpeed;


        return true;
    }


    // ============================================================
    // 潜望鏡
    // ============================================================

    /// <summary>
    /// 潜望鏡の実際の視点位置を返す。
    /// 通常はMain CameraのWorld Position。
    /// </summary>
    public static Vector3 GetPeriscopePosition()
    {
        return
            periscope_position;
    }


    public static bool SetPeriscopePosition(
        Vector3 position
    )
    {
        if (!IsFinite(position))
        {
            Debug.LogError(
                "Invalid periscope position: " +
                position
            );

            return false;
        }


        periscope_position =
            position;


        return true;
    }


    /// <summary>
    /// 潜望鏡が現在向いているWorld Yawを返す。
    /// </summary>
    public static float GetPeriscopeRotation()
    {
        return
            periscope_rotation;
    }


    public static bool SetPeriscopeRotation(
        float rotation
    )
    {
        if (!IsFinite(rotation))
        {
            Debug.LogError(
                "Invalid periscope rotation: " +
                rotation
            );

            return false;
        }


        periscope_rotation =
            NormalizeAngle(
                rotation
            );


        return true;
    }


    /// <summary>
    /// PeriscopeRootのLocal Yを返す。
    /// </summary>
    public static float GetPeriscopeLocalHeight()
    {
        return
            periscope_local_height;
    }


    public static bool SetPeriscopeLocalHeight(
        float localHeight
    )
    {
        if (!IsFinite(localHeight))
        {
            return false;
        }


        periscope_local_height =
            localHeight;


        return true;
    }


    /// <summary>
    /// 潜望鏡の視点が海面より上に露出しているか。
    /// </summary>
    public static bool GetIsPeriscopeAboveSurface()
    {
        return
            periscope_is_above_surface;
    }


    public static void SetIsPeriscopeAboveSurface(
        bool isAboveSurface
    )
    {
        periscope_is_above_surface =
            isAboveSurface;
    }


    /// <summary>
    /// 潜望鏡が上限位置まで上がっているか。
    /// </summary>
    public static bool GetIsPeriscopeFullyRaised()
    {
        return
            periscope_is_fully_raised;
    }


    public static void SetIsPeriscopeFullyRaised(
        bool isFullyRaised
    )
    {
        periscope_is_fully_raised =
            isFullyRaised;
    }


    /// <summary>
    /// 潜望鏡が下限位置まで下がっているか。
    ///
    /// 後でSignalInputControllerから
    /// 「完全に格納されている時だけButton4を有効」
    /// とするために使用する。
    /// </summary>
    public static bool GetIsPeriscopeFullyLowered()
    {
        return
            periscope_is_fully_lowered;
    }


    public static void SetIsPeriscopeFullyLowered(
        bool isFullyLowered
    )
    {
        periscope_is_fully_lowered =
            isFullyLowered;
    }


    // ============================================================
    // 敵艦・味方艦
    // ============================================================

    public static float GetEnemyShipMaxSpeed()
    {
        return
            enemyShip_max_speed;
    }


    public static bool SetEnemyShipMaxSpeed(
        float maxSpeed
    )
    {
        if (
            !IsFinite(maxSpeed) ||
            maxSpeed <
            MinimumNonNegativeValue
        )
        {
            return false;
        }


        enemyShip_max_speed =
            maxSpeed;


        return true;
    }


    public static float GetFellowShipMaxSpeed()
    {
        return
            fellowShip_max_speed;
    }


    public static bool SetFellowShipMaxSpeed(
        float maxSpeed
    )
    {
        if (
            !IsFinite(maxSpeed) ||
            maxSpeed <
            MinimumNonNegativeValue
        )
        {
            return false;
        }


        fellowShip_max_speed =
            maxSpeed;


        return true;
    }


    public static float GetEnemyShipRotateRadius()
    {
        return
            enemyShip_rotate_radius;
    }


    public static bool SetEnemyShipRotateRadius(
        float radius
    )
    {
        if (
            !IsFinite(radius) ||
            radius <
            MinimumNonNegativeValue
        )
        {
            return false;
        }


        enemyShip_rotate_radius =
            radius;


        return true;
    }


    // ============================================================
    // スコア
    // ============================================================

    public static int GetScore()
    {
        return
            score;
    }


    public static bool AddScore(
        int additionalScore
    )
    {
        score +=
            additionalScore;


        return true;
    }


    private static bool SetScore(
        int newScore
    )
    {
        score =
            newScore;


        return true;
    }


    // ============================================================
    // 敵艦リスト
    // ============================================================

    public static bool AddEnemyShip(
        string enemyShipName
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                enemyShipName
            )
        )
        {
            return false;
        }


        string[] tokens =
            enemyShipName.Split(
                '_'
            );


        if (
            tokens.Length !=
            EnemyShipNameTokenCount
        )
        {
            return false;
        }


        if (
            tokens[0] !=
            EnemyShipNamePrefix
        )
        {
            return false;
        }


        // 二重登録を防止
        if (
            enemy_ships_list.Contains(
                enemyShipName
            )
        )
        {
            return false;
        }


        enemy_ships_list.Add(
            enemyShipName
        );


        return true;
    }


    public static bool DeleteEnemyShip(
        string enemyShipName
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                enemyShipName
            )
        )
        {
            return false;
        }


        return
            enemy_ships_list.Remove(
                enemyShipName
            );
    }


    public static List<string> GetEnemyShipList()
    {
        return
            enemy_ships_list;
    }


    // ============================================================
    // 敵艦距離情報
    // ============================================================

    /// <summary>
    /// 潜水艦から各敵艦までの
    /// X方向・Z方向・距離を返す。
    ///
    /// [0] = X方向
    /// [1] = Z方向
    /// [2] = 距離
    ///
    /// この形式は現在のSonar.csとの互換性維持用。
    /// 将来的にSurfaceContactへ変更するときに
    /// 専用クラスへ置き換える予定。
    /// </summary>
    public static List<float[]>
        GetEnemyShipDistanceList()
    {
        List<float[]> resultList =
            new();


        for (
            int i = 0;
            i < enemy_ships_list.Count;
            i++
        )
        {
            GameObject enemyShip =
                GameObject.Find(
                    enemy_ships_list[i]
                );


            if (enemyShip == null)
            {
                continue;
            }


            Vector3 enemyPosition =
                enemyShip.transform.position;


            Vector3 difference =
                enemyPosition -
                submarine_position;


            float horizontalDistance =
                new Vector2(
                    difference.x,
                    difference.z
                ).magnitude;


            float[] result =
                new float[
                    DistanceDataLength
                ];


            result[
                DistanceDataDirectionXIndex
            ] =
                difference.x;


            result[
                DistanceDataDirectionZIndex
            ] =
                difference.z;


            result[
                DistanceDataDistanceIndex
            ] =
                horizontalDistance;


            resultList.Add(
                result
            );
        }


        return
            resultList;
    }


    // ============================================================
    // センサー
    // ============================================================

    // =========================
    // Yaw
    // =========================

    public static float GetSensorYaw()
    {
        return
            sensor_yaw;
    }


    public static bool SetSensorYaw(
        float yaw
    )
    {
        if (!IsFinite(yaw))
        {
            return false;
        }


        sensor_yaw =
            yaw;


        return true;
    }


    // =========================
    // Speed
    // =========================

    public static float GetSensorSpeed()
    {
        return
            sensor_speed;
    }


    public static bool SetSensorSpeed(
        float speed
    )
    {
        if (!IsFinite(speed))
        {
            return false;
        }


        sensor_speed =
            speed;


        return true;
    }


    // =========================
    // Button1
    // =========================

    public static int GetSensorButton1()
    {
        return
            sensor_button1;
    }


    public static bool SetSensorButton1(
        int value
    )
    {
        return
            SetButtonValue(
                ref sensor_button1,
                value,
                nameof(sensor_button1)
            );
    }


    // =========================
    // Button2
    // =========================

    public static int GetSensorButton2()
    {
        return
            sensor_button2;
    }


    public static bool SetSensorButton2(
        int value
    )
    {
        return
            SetButtonValue(
                ref sensor_button2,
                value,
                nameof(sensor_button2)
            );
    }


    // =========================
    // Button3
    // =========================

    public static int GetSensorButton3()
    {
        return
            sensor_button3;
    }


    public static bool SetSensorButton3(
        int value
    )
    {
        return
            SetButtonValue(
                ref sensor_button3,
                value,
                nameof(sensor_button3)
            );
    }


    // =========================
    // Button4
    // =========================

    public static int GetSensorButton4()
    {
        return
            sensor_button4;
    }


    public static bool SetSensorButton4(
        int value
    )
    {
        return
            SetButtonValue(
                ref sensor_button4,
                value,
                nameof(sensor_button4)
            );
    }


    // =========================
    // Button5
    // =========================

    public static int GetSensorButton5()
    {
        return
            sensor_button5;
    }


    public static bool SetSensorButton5(
        int value
    )
    {
        return
            SetButtonValue(
                ref sensor_button5,
                value,
                nameof(sensor_button5)
            );
    }


    // =========================
    // Button6
    // =========================

    public static int GetSensorButton6()
    {
        return
            sensor_button6;
    }


    public static bool SetSensorButton6(
        int value
    )
    {
        return
            SetButtonValue(
                ref sensor_button6,
                value,
                nameof(sensor_button6)
            );
    }


    // ============================================================
    // シーン変更確認
    // ============================================================

    public static bool GetChangeSceneConfirmation()
    {
        return
            changeSceneConfirmation;
    }


    public static void SetChangeSceneConfirmation(
        bool isEnabled
    )
    {
        changeSceneConfirmation =
            isEnabled;
    }


    // ============================================================
    // ソナー
    // ============================================================

    public static bool
        GetSonarPanelUnderwaterCanOpen()
    {
        return
            sonar_panel_underwater_canopen;
    }


    public static void
        SetSonarPanelUnderwaterCanOpen(
            bool canOpen
        )
    {
        sonar_panel_underwater_canopen =
            canOpen;
    }


    // ============================================================
    // 共通処理
    // ============================================================

    private static bool SetButtonValue(
        ref int targetButton,
        int value,
        string buttonName
    )
    {
        if (
            value != ButtonReleased &&
            value != ButtonPressed
        )
        {
            Debug.LogError(
                "Invalid button value (" +
                buttonName +
                "): " +
                value
            );

            return false;
        }


        targetButton =
            value;


        return true;
    }


    private static float NormalizeAngle(
        float angle
    )
    {
        return
            Mathf.Repeat(
                angle,
                FullRotationDegrees
            );
    }


    private static bool IsFinite(
        float value
    )
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }


    private static bool IsFinite(
        Vector3 value
    )
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }
}