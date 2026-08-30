using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ColorMemoryGameManager : MonoBehaviour
{
    // ============================================================
    // Constants
    // ============================================================

    private const float MinimumValue =
        0.0f;


    private const int UnlimitedEnemyCount =
        0;


    // ============================================================
    // Sensor
    // ============================================================

    [Header("Sensor")]

    [SerializeField]
    private SensorRead sensor;


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField]
    private ColorMemoryMissionManager
        colorMemoryMissionManager;


    // ============================================================
    // Ambient Contacts
    // ============================================================

    [Header("Surface Contacts")]

    [SerializeField]
    private AmbientContactSpawner
        ambientContactSpawner;


    [SerializeField]
    private bool resetAllContactsAfterMission =
        true;


    // ============================================================
    // Game Time
    // ============================================================

    [Header("Game Time")]

    [SerializeField]
    private float timeLimit =
        60.0f;


    [SerializeField]
    private string resultSceneName =
        "ResultScene";


    private float timeCount =
        0.0f;


    // ============================================================
    // Enemy
    // ============================================================

    [Header("Color Memory Enemy")]

    [SerializeField]
    private GameObject colorMemoryEnemyPrefab;


    [SerializeField]
    private bool spawnFirstEnemyOnStart =
        true;


    [SerializeField]
    private float initialSpawnDelay =
        1.0f;


    [SerializeField]
    private float nextSpawnDelay =
        2.0f;


    [SerializeField]
    private int maximumEnemySpawnCount =
        UnlimitedEnemyCount;


    // ============================================================
    // Spawn position
    // ============================================================

    [Header("Enemy Spawn Position")]

    [SerializeField]
    private float enemyShipSpawnDistanceMagnification =
        2.0f;


    [SerializeField]
    private float enemyShipSpawnAngleMin =
        100.0f;


    [SerializeField]
    private float enemyShipSpawnAngleMax =
        270.0f;


    [SerializeField]
    private float enemyShipWorldY =
        0.0f;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        true;


    // ============================================================
    // Internal
    // ============================================================

    private int enemyShipCount =
        0;


    private readonly List<GameObject>
        spawnedEnemyShips =
            new List<GameObject>();


    private bool waitingForRoundReset =
        false;


    private bool roundResetInProgress =
        false;


    private Coroutine initialSpawnCoroutine;


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();


        DataManager.Initialize();


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
        UpdateSensorData();

        UpdateGameTime();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        UnsubscribeMissionEvents();
    }


    // ============================================================
    // References
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
            colorMemoryMissionManager ==
            null
        )
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<
                    ColorMemoryMissionManager
                >();
        }


        if (
            ambientContactSpawner ==
            null
        )
        {
            ambientContactSpawner =
                FindFirstObjectByType<
                    AmbientContactSpawner
                >();
        }
    }


    // ============================================================
    // Events
    // ============================================================

    private void SubscribeMissionEvents()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        colorMemoryMissionManager
            .MissionEvaluated +=
                HandleMissionEvaluated;


        colorMemoryMissionManager
            .MissionStateChanged +=
                HandleMissionStateChanged;
    }


    private void UnsubscribeMissionEvents()
    {
        if (
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        colorMemoryMissionManager
            .MissionEvaluated -=
                HandleMissionEvaluated;


        colorMemoryMissionManager
            .MissionStateChanged -=
                HandleMissionStateChanged;
    }


    // ============================================================
    // Result
    // ============================================================

    private void HandleMissionEvaluated(
        bool success
    )
    {
        if (!resetAllContactsAfterMission)
        {
            return;
        }


        waitingForRoundReset =
            true;
    }


    private void HandleMissionStateChanged(
        ColorMemoryMissionManager
            .MissionState state
    )
    {
        if (
            state !=
            ColorMemoryMissionManager
                .MissionState
                .Searching
        )
        {
            return;
        }


        if (
            !waitingForRoundReset ||
            roundResetInProgress
        )
        {
            return;
        }


        StartCoroutine(
            ResetRoundRoutine()
        );
    }


    // ============================================================
    // Round
    // ============================================================

    private IEnumerator ResetRoundRoutine()
    {
        waitingForRoundReset =
            false;


        roundResetInProgress =
            true;


        ClearEnemies();


        if (
            ambientContactSpawner !=
            null
        )
        {
            ambientContactSpawner
                .ClearSpawnedContacts();
        }


        yield return null;


        if (
            nextSpawnDelay >
            0.0f
        )
        {
            yield return
                new WaitForSeconds(
                    nextSpawnDelay
                );
        }


        SpawnEnemy();


        if (
            ambientContactSpawner !=
            null
        )
        {
            ambientContactSpawner
                .SpawnInitialContacts();
        }


        roundResetInProgress =
            false;
    }


    // ============================================================
    // Initial spawn
    // ============================================================

    private IEnumerator SpawnFirstEnemyRoutine()
    {
        if (
            initialSpawnDelay >
            0.0f
        )
        {
            yield return
                new WaitForSeconds(
                    initialSpawnDelay
                );
        }


        SpawnEnemy();


        initialSpawnCoroutine =
            null;
    }


    // ============================================================
    // Spawn
    // ============================================================

    private bool SpawnEnemy()
    {
        if (
            colorMemoryEnemyPrefab ==
            null
        )
        {
            Debug.LogError(
                "ColorMemoryEnemyPrefabが設定されていません。"
            );


            return false;
        }


        if (
            maximumEnemySpawnCount !=
                UnlimitedEnemyCount
            &&
            enemyShipCount >=
                maximumEnemySpawnCount
        )
        {
            return false;
        }


        float submarineRotation =
            DataManager
                .GetSubmarineRotation();


        float angle =
            Random.Range(
                submarineRotation +
                enemyShipSpawnAngleMin,

                submarineRotation +
                enemyShipSpawnAngleMax
            );


        float distance =
            DataManager
                .GetEnemyShipRotateRadius()
            *
            enemyShipSpawnDistanceMagnification;


        Vector3 direction =
            Quaternion.Euler(
                0.0f,
                angle,
                0.0f
            )
            *
            Vector3.forward;


        Vector3 position =
            DataManager
                .GetSubmarinePosition()
            +
            direction *
            distance;


        position.y =
            enemyShipWorldY;


        GameObject enemy =
            Instantiate(
                colorMemoryEnemyPrefab,
                position,
                Quaternion.Euler(
                    0.0f,
                    angle,
                    0.0f
                )
            );


        if (
            enemy.GetComponent<
                ColorMemoryEnemyShip
            >() ==
            null
        )
        {
            Debug.LogError(
                "PrefabにColorMemoryEnemyShipがありません。"
            );


            Destroy(
                enemy
            );


            return false;
        }


        enemyShipCount++;


        string enemyName =
            "EnemyShip_" +
            enemyShipCount;


        enemy.name =
            enemyName;


        DataManager.AddEnemyShip(
            enemyName
        );


        spawnedEnemyShips.Add(
            enemy
        );


        if (debugLog)
        {
            Debug.Log(
                "ColorMemory Enemy生成: " +
                enemyName
            );
        }


        return true;
    }


    // ============================================================
    // Clear
    // ============================================================

    private void ClearEnemies()
    {
        for (
            int i =
                spawnedEnemyShips.Count - 1;

            i >= 0;

            i--
        )
        {
            GameObject enemy =
                spawnedEnemyShips[i];


            if (enemy == null)
            {
                continue;
            }


            DataManager.DeleteEnemyShip(
                enemy.name
            );


            enemy.SetActive(
                false
            );


            Destroy(
                enemy
            );
        }


        spawnedEnemyShips.Clear();
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
    // Time
    // ============================================================

    private void UpdateGameTime()
    {
        if (timeLimit <= 0.0f)
        {
            return;
        }


        timeCount +=
            Time.deltaTime;


        if (
            timeCount <
            timeLimit
        )
        {
            return;
        }


        SceneManager.LoadScene(
            resultSceneName
        );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        timeLimit =
            Mathf.Max(
                MinimumValue,
                timeLimit
            );


        initialSpawnDelay =
            Mathf.Max(
                MinimumValue,
                initialSpawnDelay
            );


        nextSpawnDelay =
            Mathf.Max(
                MinimumValue,
                nextSpawnDelay
            );


        enemyShipSpawnDistanceMagnification =
            Mathf.Max(
                MinimumValue,
                enemyShipSpawnDistanceMagnification
            );


        maximumEnemySpawnCount =
            Mathf.Max(
                0,
                maximumEnemySpawnCount
            );
    }
}