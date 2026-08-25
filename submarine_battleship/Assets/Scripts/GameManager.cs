using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultTimeLimit =
        60.0f;

    private const float UnlimitedTimeValue =
        0.0f;

    private const float DefaultInitialSpawnDelay =
        1.0f;

    private const float DefaultNextSpawnDelay =
        2.0f;

    private const float DefaultSpawnDistanceMagnification =
        2.0f;

    private const float DefaultSpawnAngleMin =
        100.0f;

    private const float DefaultSpawnAngleMax =
        270.0f;

    private const float DefaultEnemyWorldY =
        0.0f;

    private const float MinimumNonNegativeValue =
        0.0f;

    private const int UnlimitedEnemySpawnCount =
        0;

    private const string EnemyShipNamePrefix =
        "EnemyShip_";

    private const string ResultSceneName =
        "ResultScene";


    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField, Tooltip(
        "Raspberry Piからの値を受信するSensorRead")]
    private SensorRead sensor;


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField, Tooltip(
        "通信ミッションを管理するManager")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // Ambient Contacts
    // ============================================================

    [Header("Surface Contacts")]

    [SerializeField, Tooltip(
        "Friendly / Neutralの生成を管理するSpawner")]
    private AmbientContactSpawner
        ambientContactSpawner;


    [SerializeField, Tooltip(
        "通信成功・失敗後に3種類の船をすべて入れ替える")]
    private bool resetAllContactsAfterMission =
        true;


    // ============================================================
    // ゲーム時間
    // ============================================================

    [Header("Game Time")]

    [SerializeField, Tooltip(
        "ゲーム制限時間。0なら無制限")]
    private float time_limit =
        DefaultTimeLimit;


    private float time_count =
        MinimumNonNegativeValue;


    // ============================================================
    // Enemy
    // ============================================================

    [Header("Enemy Ship")]

    [SerializeField, Tooltip(
        "雪風のEnemy用Prefab")]
    private GameObject enemyShipPrefab;


    [SerializeField, Tooltip(
        "ゲーム開始時に最初のEnemyを生成する")]
    private bool spawnFirstEnemyOnStart =
        true;


    [SerializeField, Tooltip(
        "ゲーム開始から最初のEnemyを生成するまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float initialSpawnDelay =
        DefaultInitialSpawnDelay;


    [SerializeField, Tooltip(
        "1ラウンド終了後、新しい3隻を生成するまでの時間")]
    [Min(MinimumNonNegativeValue)]
    private float nextSpawnDelay =
        DefaultNextSpawnDelay;


    [SerializeField, Tooltip(
        "ゲーム全体で生成可能なEnemy数。" +
        "0なら無制限")]
    [Min(UnlimitedEnemySpawnCount)]
    private int maximumEnemySpawnCount =
        UnlimitedEnemySpawnCount;


    // ============================================================
    // Enemy配置
    // ============================================================

    [Header("Enemy Spawn Position")]

    [SerializeField, Tooltip(
        "敵艦回転半径に対するスポーン距離倍率")]
    [Min(MinimumNonNegativeValue)]
    private float enemyShipSpawnDistanceMagnification =
        DefaultSpawnDistanceMagnification;


    [SerializeField, Tooltip(
        "潜水艦正面を基準にした最小スポーン角度")]
    private float enemyShipSpawnAngleMin =
        DefaultSpawnAngleMin;


    [SerializeField, Tooltip(
        "潜水艦正面を基準にした最大スポーン角度")]
    private float enemyShipSpawnAngleMax =
        DefaultSpawnAngleMax;


    [SerializeField, Tooltip(
        "EnemyのワールドY座標")]
    private float enemyShipWorldY =
        DefaultEnemyWorldY;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        true;


    // ============================================================
    // 内部状態
    // ============================================================

    // Enemy名の連番兼、総生成数
    private int enemyShipCount =
        0;


    // GameManager自身が生成したEnemy
    private readonly List<GameObject>
        spawnedEnemyShips =
            new List<GameObject>();


    private bool waitingForRoundReset =
        false;


    private bool roundResetInProgress =
        false;


    private Coroutine initialSpawnCoroutine;

    private Coroutine roundResetCoroutine;


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
        ResolveReferences();


        DataManager.Initialize();


        time_count =
            MinimumNonNegativeValue;


        SubscribeMissionEvents();


        if (spawnFirstEnemyOnStart)
        {
            initialSpawnCoroutine =
                StartCoroutine(
                    SpawnFirstEnemyRoutine()
                );
        }
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        UpdateGameTime();

        UpdateSensorData();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeMissionEvents();


        if (initialSpawnCoroutine != null)
        {
            StopCoroutine(
                initialSpawnCoroutine
            );
        }


        if (roundResetCoroutine != null)
        {
            StopCoroutine(
                roundResetCoroutine
            );
        }
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (sensor == null)
        {
            sensor =
                FindFirstObjectByType<
                    SensorRead
                >();
        }


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


        if (ambientContactSpawner == null)
        {
            ambientContactSpawner =
                FindFirstObjectByType<
                    AmbientContactSpawner
                >();
        }


        if (sensor == null)
        {
            Debug.LogWarning(
                "SensorReadが見つかりません。"
            );
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


        if (ambientContactSpawner == null)
        {
            Debug.LogWarning(
                "AmbientContactSpawnerが見つかりません。"
            );
        }
    }


    // ============================================================
    // Mission Event
    // ============================================================

    private void SubscribeMissionEvents()
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


    private void UnsubscribeMissionEvents()
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
    // 正誤判定終了
    // ============================================================

    private void HandleMissionEvaluated(
        bool wasSuccessful
    )
    {
        if (!resetAllContactsAfterMission)
        {
            return;
        }


        // 成功・失敗を問わず
        // 次回Searching時に海上接触をリセット
        waitingForRoundReset =
            true;


        if (debugLog)
        {
            Debug.Log(
                "通信結果: " +
                (
                    wasSuccessful
                        ? "成功"
                        : "失敗"
                ) +
                " / 次のラウンドで全船を更新します。"
            );
        }
    }


    // ============================================================
    // Mission State
    // ============================================================

    private void HandleMissionStateChanged(
        CommunicationMissionManager.MissionState
            newState
    )
    {
        if (
            newState !=
            CommunicationMissionManager
                .MissionState
                .Searching
        )
        {
            return;
        }


        if (!waitingForRoundReset)
        {
            return;
        }


        if (roundResetInProgress)
        {
            return;
        }


        roundResetCoroutine =
            StartCoroutine(
                ResetRoundRoutine()
            );
    }


    // ============================================================
    // ラウンドリセット
    // ============================================================

    private IEnumerator ResetRoundRoutine()
    {
        waitingForRoundReset =
            false;


        roundResetInProgress =
            true;


        // ========================================================
        // 現在の3種類の船を削除
        // ========================================================

        ClearCurrentRoundContacts();


        // Destroy処理をUnityへ反映するため
        // 1フレーム待つ
        yield return null;


        // ========================================================
        // 次ラウンドまで待機
        // ========================================================

        if (
            nextSpawnDelay >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    nextSpawnDelay
                );
        }


        // ========================================================
        // 新しいラウンド生成
        // ========================================================

        SpawnNewRound();


        roundResetInProgress =
            false;


        roundResetCoroutine =
            null;
    }


    // ============================================================
    // 現在ラウンド削除
    // ============================================================

    private void ClearCurrentRoundContacts()
    {
        int enemyRemoved =
            ClearSpawnedEnemyShips();


        int ambientRemoved =
            0;


        if (ambientContactSpawner != null)
        {
            ambientRemoved =
                ambientContactSpawner
                    .ClearSpawnedContacts();
        }


        if (debugLog)
        {
            Debug.Log(
                "海上接触をリセットしました。" +
                " Enemy=" +
                enemyRemoved +
                " / Friendly・Neutral=" +
                ambientRemoved
            );
        }
    }


    // ============================================================
    // Enemy全削除
    // ============================================================

    private int ClearSpawnedEnemyShips()
    {
        CleanupEnemyShipList();


        int removedCount =
            0;


        for (
            int index = spawnedEnemyShips.Count - 1;
            index >= 0;
            index--
        )
        {
            GameObject enemyShip =
                spawnedEnemyShips[index];


            if (enemyShip == null)
            {
                continue;
            }


            // 古いDataManager登録も消しておく
            DataManager.DeleteEnemyShip(
                enemyShip.name
            );


            // SurfaceContactから即時登録解除
            enemyShip.SetActive(
                false
            );


            Destroy(
                enemyShip
            );


            removedCount++;
        }


        spawnedEnemyShips.Clear();


        return
            removedCount;
    }


    // ============================================================
    // Enemy List掃除
    // ============================================================

    private void CleanupEnemyShipList()
    {
        for (
            int index = spawnedEnemyShips.Count - 1;
            index >= 0;
            index--
        )
        {
            if (
                spawnedEnemyShips[index] ==
                null
            )
            {
                spawnedEnemyShips
                    .RemoveAt(
                        index
                    );
            }
        }
    }


    // ============================================================
    // 新しいラウンド
    // ============================================================

    private void SpawnNewRound()
    {
        // 次のEnemyを生成できないなら、
        // 新しいラウンド自体を開始しない
        if (!CanSpawnEnemyShip())
        {
            if (debugLog)
            {
                Debug.Log(
                    "Enemy生成上限に到達したため、" +
                    "新しいラウンドを生成しません。"
                );
            }


            return;
        }


        // Enemy
        bool enemySpawned =
            SpawnEnemyShip();


        // Friendly / Neutral
        if (ambientContactSpawner != null)
        {
            ambientContactSpawner
                .SpawnInitialContacts();
        }


        if (debugLog)
        {
            Debug.Log(
                enemySpawned
                    ? "新しい3種類の海上接触を生成しました。"
                    : "Enemyの生成に失敗しました。"
            );
        }
    }


    // ============================================================
    // 最初のEnemy
    // ============================================================

    private IEnumerator SpawnFirstEnemyRoutine()
    {
        if (
            initialSpawnDelay >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    initialSpawnDelay
                );
        }


        SpawnEnemyShip();


        initialSpawnCoroutine =
            null;
    }


    // ============================================================
    // Enemy生成可能か
    // ============================================================

    private bool CanSpawnEnemyShip()
    {
        if (
            maximumEnemySpawnCount ==
            UnlimitedEnemySpawnCount
        )
        {
            return true;
        }


        return
            enemyShipCount <
            maximumEnemySpawnCount;
    }


    // ============================================================
    // Enemy生成
    // ============================================================

    private bool SpawnEnemyShip()
    {
        if (!CanSpawnEnemyShip())
        {
            return false;
        }


        if (enemyShipPrefab == null)
        {
            Debug.LogError(
                "Enemy Ship Prefabが設定されていません。"
            );


            return false;
        }


        float submarineRotation =
            DataManager
                .GetSubmarineRotation();


        float spawnAngleY =
            Random.Range(
                submarineRotation +
                enemyShipSpawnAngleMin,

                submarineRotation +
                enemyShipSpawnAngleMax
            );


        float spawnDistance =
            DataManager
                .GetEnemyShipRotateRadius()
            *
            enemyShipSpawnDistanceMagnification;


        Vector3 spawnDirection =
            Quaternion.Euler(
                MinimumNonNegativeValue,
                spawnAngleY,
                MinimumNonNegativeValue
            )
            *
            Vector3.forward;


        Vector3 spawnPosition =
            DataManager
                .GetSubmarinePosition()
            +
            spawnDirection *
            spawnDistance;


        spawnPosition.y =
            enemyShipWorldY;


        Quaternion spawnRotation =
            Quaternion.Euler(
                MinimumNonNegativeValue,
                spawnAngleY,
                MinimumNonNegativeValue
            );


        GameObject enemyShip =
            Instantiate(
                enemyShipPrefab,
                spawnPosition,
                spawnRotation
            );


        if (enemyShip == null)
        {
            return false;
        }


        enemyShipCount++;


        string enemyShipName =
            EnemyShipNamePrefix +
            enemyShipCount;


        enemyShip.name =
            enemyShipName;


        DataManager.AddEnemyShip(
            enemyShipName
        );


        spawnedEnemyShips.Add(
            enemyShip
        );


        if (debugLog)
        {
            Debug.Log(
                "Enemyを生成しました: " +
                enemyShipName +
                " / Position=" +
                spawnPosition
            );
        }


        return true;
    }


    // ============================================================
    // Sensor
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
    // ゲーム時間
    // ============================================================

    private void UpdateGameTime()
    {
        if (
            time_limit ==
            UnlimitedTimeValue
        )
        {
            return;
        }


        time_count +=
            Time.deltaTime;


        if (
            time_count <
            time_limit
        )
        {
            return;
        }


        SceneManager.LoadScene(
            ResultSceneName
        );
    }


    // ============================================================
    // Inspector値
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    private void ValidateSettings()
    {
        if (
            time_limit <
            MinimumNonNegativeValue
        )
        {
            time_limit =
                DefaultTimeLimit;
        }


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


        maximumEnemySpawnCount =
            Mathf.Max(
                UnlimitedEnemySpawnCount,
                maximumEnemySpawnCount
            );


        enemyShipSpawnDistanceMagnification =
            Mathf.Max(
                MinimumNonNegativeValue,
                enemyShipSpawnDistanceMagnification
            );


        if (
            enemyShipSpawnAngleMax <
            enemyShipSpawnAngleMin
        )
        {
            float temporary =
                enemyShipSpawnAngleMin;


            enemyShipSpawnAngleMin =
                enemyShipSpawnAngleMax;


            enemyShipSpawnAngleMax =
                temporary;
        }
    }
}