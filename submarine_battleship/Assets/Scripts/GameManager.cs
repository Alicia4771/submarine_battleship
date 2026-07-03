using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float senseor_value;
    [SerializeField] private SensorRead sensor;
    [SerializeField] private float time_limit;
    private float default_time_limit = 60f;
    private float time_count = 0f;
    private float time_count_before = 0f;

    [SerializeField, Tooltip("敵艦のプレハブ")] private GameObject enemyShipPrefab;
    private float enemyShipSpawnDistanceMagnification = 2f;     // 敵艦のスポーン距離を潜水艦の半径の何倍にするか
    private float enemyShipSpawnAngleMin = 100f;                 // 敵艦のスポーン角度の最小値
    private float enemyShipSpawnAngleMax = 270f;                 // 敵艦のスポーン角度の最大値
    private int enemyShipCount = 0;

    void Awake()
    {
        if (time_limit < 0) time_limit = default_time_limit;
        if (time_limit == 0) time_limit = float.MaxValue;
    }
    
    void Start()
    {
        sensor = FindFirstObjectByType<SensorRead>();

        DataManager.Initialize();
        time_count = 0f;
        time_count_before = 0f;
    }

    void Update()
    {
        time_count += Time.deltaTime;
        senseor_value = sensor.GetYaw(); // センサー値を取得

        if (time_count_before < 3 &&time_count >= 3)
        {
            SpawnEnemyShip();
        }
        if (time_count_before < 5 &&time_count >= 5)
        {
            SpawnEnemyShip();
        }
        if (time_count_before < 7 &&time_count >= 7)
        {
            SpawnEnemyShip();
        }
        Debug.Log("sensor : " + senseor_value);

        time_count_before = time_count;
    }


    private bool SpawnEnemyShip()
    {
        if (enemyShipPrefab == null)
        {
            Debug.LogError("Enemy ship prefab is not assigned.");
            return false;
        }

        // 潜水艦の現在の向き
        float submarineRotation = DataManager.GetSubmarineRotation();
        // 敵船のスポーン角度をランダムに決定
        float spawnAngleY = Random.Range(submarineRotation + enemyShipSpawnAngleMin, submarineRotation + enemyShipSpawnAngleMax);
        // 敵船の向き
        Quaternion spawnRotation = Quaternion.Euler(0f, spawnAngleY, 0f);
        // 敵船のスポーン距離
        float spawnDistance = DataManager.GetEnemyShipRotateRadius() * enemyShipSpawnDistanceMagnification;

        // 敵船のスポーン方向
        Vector3 spawnDirection = Quaternion.Euler(0f, spawnAngleY, 0f) * Vector3.forward;

        // 敵船のスポーン位置
        Vector3 spawnPosition = DataManager.GetSubmarinePosition() + spawnDirection * spawnDistance;

        GameObject enemyShip = Instantiate(enemyShipPrefab, spawnPosition, spawnRotation);
        string enemyShipName = "EnemyShip_" + (++enemyShipCount);
        enemyShip.name = enemyShipName;
        DataManager.AddEnemyShip(enemyShipName);

        if (enemyShip == null)
        {
            Debug.LogError("Failed to instantiate enemy ship.");
            return false;
        }

        return true;
    }
}