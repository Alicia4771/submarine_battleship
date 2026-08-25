using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultTimeLimit = 60.0f;

    private const float DefaultInitialSpawnDelay = 1.0f;
    private const float DefaultNextSpawnDelay = 2.0f;

    private const float DefaultSpawnDistanceMagnification = 2.0f;

    private const float DefaultSpawnAngleMin = 0.0f;
    private const float DefaultSpawnAngleMax = 360.0f;

    private const float DefaultEnemyShipWorldY = 0.0f;

    private const float MinimumNonNegativeValue = 0.0f;

    private const int UnlimitedSpawnCount = 0;

    private const string EnemyShipNamePrefix = "EnemyShip_";

    private const string ResultSceneName = "ResultScene";


    // ============================================================
    // センサー
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "Raspberry Piから受信したセンサーデータを管理するSensorRead")]
    private SensorRead sensor;


    // ============================================================
    // 通信システム
    // ============================================================

    [Header("Communication")]

    [SerializeField, Tooltip(
        "通信ミッションを管理するCommunicationMissionManager。" +
        "未設定の場合はシーン内から自動検索する")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // ゲーム時間
    // ============================================================

    [Header("Game Time")]

    [SerializeField, Tooltip(
        "ゲームの制限時間。" +
        "0の場合は時間制限なし")]
    [Min(MinimumNonNegativeValue)]
    private float timeLimit =
        DefaultTimeLimit;


    // ============================================================
    // 敵艦Prefab
    // ============================================================

    [Header("Enemy Ship")]

    [SerializeField, Tooltip(
        "生成する敵艦のPrefab。" +
        "雪風Prefabなどを設定する")]
    private GameObject enemyShipPrefab;


    // ============================================================
    // 敵艦スポーンタイミング
    // ============================================================

    [Header("Enemy Spawn Timing")]

    [SerializeField, Tooltip(
        "ゲーム開始時にGameManagerから最初の敵艦を生成するか。" +
        "Hierarchyに雪風を直接置いてテストする場合はOFF")]
    private bool spawnFirstEnemyOnStart = false;


    [SerializeField, Tooltip(
        "ゲーム開始後、最初の敵艦を生成するまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float initialSpawnDelay =
        DefaultInitialSpawnDelay;


    [SerializeField, Tooltip(
        "1つの通信ミッション終了後、" +
        "次の敵艦を生成するまでの待ち時間")]
    [Min(MinimumNonNegativeValue)]
    private float nextSpawnDelay =
        DefaultNextSpawnDelay;


    [SerializeField, Tooltip(
        "生成する敵艦数の上限。" +
        "0の場合は制限なし")]
    [Min(0)]
    private int maximumEnemySpawnCount =
        UnlimitedSpawnCount;


    // ============================================================
    // 敵艦スポーン位置
    // ============================================================

    [Header("Enemy Spawn Position")]

    [SerializeField, Tooltip(
        "敵艦のスポーン距離を、" +
        "DataManagerの敵艦回転半径の何倍にするか")]
    [Min(MinimumNonNegativeValue)]
    private float enemyShipSpawnDistanceMagnification =
        DefaultSpawnDistanceMagnification;


    [SerializeField, Tooltip(
        "潜水艦の進行方向を0度とした、" +
        "敵艦スポーン角度の最小値")]
    private float enemyShipSpawnAngleMin =
        DefaultSpawnAngleMin;


    [SerializeField, Tooltip(
        "潜水艦の進行方向を0度とした、" +
        "敵艦スポーン角度の最大値")]
    private float enemyShipSpawnAngleMax =
        DefaultSpawnAngleMax;


    [SerializeField, Tooltip(
        "生成する敵艦のWorld Y座標。" +
        "海面上の船の高さに合わせて設定する")]
    private float enemyShipWorldY =
        DefaultEnemyShipWorldY;


    // ============================================================
    // デバッグ
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "敵艦生成などの情報をConsoleに表示する")]
    private bool debugLog = true;


    // ============================================================
    // 内部状態
    // ============================================================

    private float timeCount = 0.0f;

    private int enemyShipCount = 0;

    private bool isGameEnding = false;

    // 通信成功・失敗後、
    // CommunicationMissionManagerがSearchingへ戻るのを待つ
    private bool waitingForNextEnemySpawn = false;

    private Coroutine enemySpawnCoroutine;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ValidateSettings();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        // =========================
        // SensorRead取得
        // =========================

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


        // =========================
        // CommunicationMissionManager取得
        // =========================

        if (
            communicationMissionManager ==
            null
        )
        {
            communicationMissionManager =
                FindFirstObjectByType<
                    CommunicationMissionManager
                >();
        }


        if (
            communicationMissionManager ==
            null
        )
        {
            Debug.LogError(
                "CommunicationMissionManagerが見つかりません。"
            );
        }
        else
        {
            SubscribeCommunicationEvents();
        }


        // =========================
        // DataManager初期化
        // =========================

        DataManager.Initialize();


        // =========================
        // 内部状態初期化
        // =========================

        timeCount =
            0.0f;

        enemyShipCount =
            0;

        isGameEnding =
            false;

        waitingForNextEnemySpawn =
            false;


        // =========================
        // 最初の敵艦
        // =========================

        if (spawnFirstEnemyOnStart)
        {
            ScheduleEnemySpawn(
                initialSpawnDelay
            );
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (isGameEnding)
        {
            return;
        }


        // =========================
        // 経過時間
        // =========================

        timeCount +=
            Time.deltaTime;


        // =========================
        // センサー更新
        // =========================

        UpdateSensorData();


        // =========================
        // ゲーム終了
        // =========================

        if (
            timeLimit >
            MinimumNonNegativeValue &&
            timeCount >=
            timeLimit
        )
        {
            EndGame();
        }
    }


    // ============================================================
    // OnDestroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeCommunicationEvents();


        if (
            enemySpawnCoroutine !=
            null
        )
        {
            StopCoroutine(
                enemySpawnCoroutine
            );

            enemySpawnCoroutine =
                null;
        }
    }


    // ============================================================
    // CommunicationMissionManagerイベント登録
    // ============================================================

    private void SubscribeCommunicationEvents()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        communicationMissionManager
            .MissionEvaluated +=
                HandleMissionEvaluated;


        communicationMissionManager
            .MissionStateChanged +=
                HandleMissionStateChanged;
    }


    private void UnsubscribeCommunicationEvents()
    {
        if (
            communicationMissionManager ==
            null
        )
        {
            return;
        }


        communicationMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;


        communicationMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;
    }


    // ============================================================
    // 通信結果
    // ============================================================

    /// <summary>
    /// 通信成功・失敗の判定が完了した時に呼ばれる。
    ///
    /// この時点ではCommunicationMissionManagerが
    /// Success / Failed状態なので、
    /// すぐには次の敵艦を生成しない。
    /// </summary>
    private void HandleMissionEvaluated(
        bool success
    )
    {
        if (isGameEnding)
        {
            return;
        }


        waitingForNextEnemySpawn =
            true;


        if (debugLog)
        {
            Debug.Log(
                success
                    ? "通信成功。次の敵艦生成を待機します。"
                    : "通信失敗。次の敵艦生成を待機します。"
            );
        }
    }


    // ============================================================
    // 通信状態変更
    // ============================================================

    /// <summary>
    /// CommunicationMissionManagerがSearchingへ戻ったら、
    /// 次の敵艦のスポーン予約を行う。
    /// </summary>
    private void HandleMissionStateChanged(
        CommunicationMissionManager.MissionState
            newState
    )
    {
        if (isGameEnding)
        {
            return;
        }


        if (
            newState !=
            CommunicationMissionManager
                .MissionState
                .Searching
        )
        {
            return;
        }


        if (!waitingForNextEnemySpawn)
        {
            return;
        }


        waitingForNextEnemySpawn =
            false;


        ScheduleEnemySpawn(
            nextSpawnDelay
        );
    }


    // ============================================================
    // センサー値更新
    // ============================================================

    private void UpdateSensorData()
    {
        if (sensor == null)
        {
            return;
        }


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
    // 敵艦生成予約
    // ============================================================

    private void ScheduleEnemySpawn(
        float delay
    )
    {
        if (isGameEnding)
        {
            return;
        }


        if (!CanSpawnMoreEnemies())
        {
            if (debugLog)
            {
                Debug.Log(
                    "敵艦の最大生成数に到達しました。"
                );
            }

            return;
        }


        // 二重予約防止
        if (
            enemySpawnCoroutine !=
            null
        )
        {
            return;
        }


        enemySpawnCoroutine =
            StartCoroutine(
                SpawnEnemyAfterDelay(
                    delay
                )
            );
    }


    // ============================================================
    // 敵艦生成待機
    // ============================================================

    private IEnumerator SpawnEnemyAfterDelay(
        float delay
    )
    {
        if (
            delay >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    delay
                );
        }


        if (!isGameEnding)
        {
            SpawnEnemyShip();
        }


        enemySpawnCoroutine =
            null;
    }


    // ============================================================
    // 生成数確認
    // ============================================================

    private bool CanSpawnMoreEnemies()
    {
        // 0なら無制限
        if (
            maximumEnemySpawnCount ==
            UnlimitedSpawnCount
        )
        {
            return true;
        }


        return
            enemyShipCount <
            maximumEnemySpawnCount;
    }


    // ============================================================
    // 敵艦スポーン
    // ============================================================

    private bool SpawnEnemyShip()
    {
        if (enemyShipPrefab == null)
        {
            Debug.LogError(
                "Enemy Ship Prefabが設定されていません。"
            );

            return false;
        }


        if (!CanSpawnMoreEnemies())
        {
            return false;
        }


        // =========================
        // 潜水艦の現在方向
        // =========================

        float submarineRotation =
            DataManager
                .GetSubmarineRotation();


        // =========================
        // スポーン角度
        // =========================

        float relativeSpawnAngle =
            Random.Range(
                enemyShipSpawnAngleMin,
                enemyShipSpawnAngleMax
            );


        float worldSpawnAngle =
            submarineRotation +
            relativeSpawnAngle;


        // =========================
        // 敵船の向き
        // =========================

        Quaternion spawnRotation =
            Quaternion.Euler(
                0.0f,
                worldSpawnAngle,
                0.0f
            );


        // =========================
        // スポーン距離
        // =========================

        float spawnDistance =
            DataManager
                .GetEnemyShipRotateRadius()
            *
            enemyShipSpawnDistanceMagnification;


        // =========================
        // スポーン方向
        // =========================

        Vector3 spawnDirection =
            Quaternion.Euler(
                0.0f,
                worldSpawnAngle,
                0.0f
            )
            *
            Vector3.forward;


        // =========================
        // スポーン位置
        // =========================

        Vector3 submarinePosition =
            DataManager
                .GetSubmarinePosition();


        Vector3 spawnPosition =
            submarinePosition +
            spawnDirection *
            spawnDistance;


        // 潜水艦は水中にいるため、
        // 敵艦の高さは別に設定する
        spawnPosition.y =
            enemyShipWorldY;


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
                "敵艦の生成に失敗しました。"
            );

            return false;
        }


        // =========================
        // 名前
        // =========================

        enemyShipCount++;


        string enemyShipName =
            EnemyShipNamePrefix +
            enemyShipCount;


        enemyShip.name =
            enemyShipName;


        // =========================
        // DataManager登録
        // =========================

        bool registered =
            DataManager.AddEnemyShip(
                enemyShipName
            );


        if (
            !registered &&
            debugLog
        )
        {
            Debug.LogWarning(
                "DataManagerへの敵艦登録に失敗しました: " +
                enemyShipName
            );
        }


        // =========================
        // デバッグ
        // =========================

        if (debugLog)
        {
            Debug.Log(
                "次の敵艦を生成しました: " +
                enemyShipName +
                " / Position = " +
                spawnPosition
            );
        }


        return true;
    }


    // ============================================================
    // ゲーム終了
    // ============================================================

    private void EndGame()
    {
        if (isGameEnding)
        {
            return;
        }


        isGameEnding =
            true;


        if (
            enemySpawnCoroutine !=
            null
        )
        {
            StopCoroutine(
                enemySpawnCoroutine
            );

            enemySpawnCoroutine =
                null;
        }


        Debug.Log(
            "Time's up! Game Over."
        );


        SceneManager.LoadScene(
            ResultSceneName
        );
    }


    // ============================================================
    // Inspector値検証
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    private void ValidateSettings()
    {
        timeLimit =
            Mathf.Max(
                MinimumNonNegativeValue,
                timeLimit
            );


        initialSpawnDelay =
            Mathf.Max(
                MinimumNonNegativeValue,
                initialSpawnDelay
            );


        nextSpawnDelay =
            Mathf.Max(
                MinimumNonNegativeValue,
                nextSpawnDelay
            );


        enemyShipSpawnDistanceMagnification =
            Mathf.Max(
                MinimumNonNegativeValue,
                enemyShipSpawnDistanceMagnification
            );


        maximumEnemySpawnCount =
            Mathf.Max(
                0,
                maximumEnemySpawnCount
            );


        // 最大角度と最小角度が逆なら入れ替える
        if (
            enemyShipSpawnAngleMax <
            enemyShipSpawnAngleMin
        )
        {
            float temporaryAngle =
                enemyShipSpawnAngleMin;


            enemyShipSpawnAngleMin =
                enemyShipSpawnAngleMax;


            enemyShipSpawnAngleMax =
                temporaryAngle;
        }
    }
}