using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // =========================
    // センサー
    // =========================

    [Header("Sensor")]
    [SerializeField] private SensorRead sensor;


    // =========================
    // ゲーム時間
    // =========================

    [SerializeField] private float time_limit;

    private float default_time_limit = 60f;

    private float time_count = 0f;
    private float time_count_before = 0f;


    // =========================
    // 敵艦
    // =========================

    [SerializeField, Tooltip("敵艦のプレハブ")]
    private GameObject enemyShipPrefab;


    // 敵艦のスポーン距離を
    // 潜水艦の半径の何倍にするか
    private float enemyShipSpawnDistanceMagnification = 2f;


    // 敵艦のスポーン角度
    private float enemyShipSpawnAngleMin = 100f;
    private float enemyShipSpawnAngleMax = 270f;


    private int enemyShipCount = 0;


    // =========================
    // Awake
    // =========================

    void Awake()
    {
        if (time_limit < 0)
        {
            time_limit = default_time_limit;
        }


        if (time_limit == 0)
        {
            time_limit = float.MaxValue;
        }
    }


    // =========================
    // Start
    // =========================

    void Start()
    {
        // InspectorでSensorReadが設定されていない場合、
        // シーン内から自動的に探す
        if (sensor == null)
        {
            sensor =
                FindFirstObjectByType<SensorRead>();
        }


        if (sensor == null)
        {
            Debug.LogError(
                "SensorReadが見つかりません。"
            );
        }


        // DataManager初期化
        DataManager.Initialize();


        // 時間初期化
        time_count = 0f;
        time_count_before = 0f;
    }


    // =========================
    // Update
    // =========================

    void Update()
    {
        // =========================
        // 経過時間
        // =========================

        time_count += Time.deltaTime;


        // =========================
        // センサー値更新
        // =========================

        UpdateSensorData();


        // =========================
        // 敵艦スポーン
        // =========================

        if (
            time_count_before < 3 &&
            time_count >= 3
        )
        {
            SpawnEnemyShip();
        }


        if (
            time_count_before < 5 &&
            time_count >= 5
        )
        {
            SpawnEnemyShip();
        }


        if (
            time_count_before < 7 &&
            time_count >= 7
        )
        {
            SpawnEnemyShip();
        }


        time_count_before = time_count;


        // =========================
        // ゲーム終了
        // =========================

        if (time_count >= time_limit)
        {
            Debug.Log(
                "Time's up! Game Over."
            );


            SceneManager.LoadScene(
                "ResultScene"
            );
        }
    }


    // ============================================================
    // SensorRead → DataManager
    // ============================================================

    private void UpdateSensorData()
    {
        // SensorReadが存在しない場合は何もしない
        if (sensor == null)
        {
            return;
        }


        // =========================
        // SensorReadから
        // 1組のセンサーデータを取得
        // =========================

        sensor.GetSensorData(
            out float yaw,
            out float speed,
            out int button1,
            out int button2,
            out int button3,
            out int button4,
            out int button5,
            out int button6
        );


        // =========================
        // DataManagerに保存
        // =========================

        DataManager.SetSensorYaw(
            yaw
        );


        DataManager.SetSensorSpeed(
            speed
        );


        DataManager.SetSensorButton1(
            button1
        );


        DataManager.SetSensorButton2(
            button2
        );


        DataManager.SetSensorButton3(
            button3
        );


        DataManager.SetSensorButton4(
            button4
        );


        DataManager.SetSensorButton5(
            button5
        );


        DataManager.SetSensorButton6(
            button6
        );
    }


    // ============================================================
    // 敵艦スポーン
    // ============================================================

    private bool SpawnEnemyShip()
    {
        if (enemyShipPrefab == null)
        {
            Debug.LogError(
                "Enemy ship prefab is not assigned."
            );

            return false;
        }


        // =========================
        // 潜水艦の現在の向き
        // =========================

        float submarineRotation =
            DataManager.GetSubmarineRotation();


        // =========================
        // 敵船のスポーン角度を
        // ランダムに決定
        // =========================

        float spawnAngleY =
            Random.Range(
                submarineRotation +
                enemyShipSpawnAngleMin,

                submarineRotation +
                enemyShipSpawnAngleMax
            );


        // =========================
        // 敵船の向き
        // =========================

        Quaternion spawnRotation =
            Quaternion.Euler(
                0f,
                spawnAngleY,
                0f
            );


        // =========================
        // 敵船のスポーン距離
        // =========================

        float spawnDistance =
            DataManager.GetEnemyShipRotateRadius()
            *
            enemyShipSpawnDistanceMagnification;


        // =========================
        // 敵船のスポーン方向
        // =========================

        Vector3 spawnDirection =
            Quaternion.Euler(
                0f,
                spawnAngleY,
                0f
            )
            *
            Vector3.forward;


        // =========================
        // 敵船のスポーン位置
        // =========================

        Vector3 spawnPosition =
            DataManager.GetSubmarinePosition()
            +
            spawnDirection
            *
            spawnDistance;


        // =========================
        // 敵船生成
        // =========================

        GameObject enemyShip =
            Instantiate(
                enemyShipPrefab,
                spawnPosition,
                spawnRotation
            );


        if (enemyShip == null)
        {
            Debug.LogError(
                "Failed to instantiate enemy ship."
            );

            return false;
        }


        // =========================
        // 敵船の名前
        // =========================

        string enemyShipName =
            "EnemyShip_" +
            (++enemyShipCount);


        enemyShip.name =
            enemyShipName;


        DataManager.AddEnemyShip(
            enemyShipName
        );


        return true;
    }
}