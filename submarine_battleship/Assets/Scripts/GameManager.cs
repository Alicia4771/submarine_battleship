using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float time_limit;
    private float default_time_limit = 60f;
    private float time_count = 0f;

    [SerializeField, Tooltip("敵艦のプレハブ")] private GameObject enemyShipPrefab;
    private int enemyShipCount = 0;

    void Awake()
    {
        if (time_limit < 0) time_limit = default_time_limit;
        if (time_limit == 0) time_limit = float.MaxValue;
    }
    
    void Start()
    {
        time_count = 0;
    }

    void Update()
    {
        time_count += Time.deltaTime;

        if (time_count >= 3)
        {
            SpawnEnemyShip();
        }
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
        float spawnAngleY = Random.Range(submarineRotation + 180f, submarineRotation + 270f);
        // 敵船の向き
        Quaternion spawnRotation = Quaternion.Euler(0f, spawnAngleY, 0f);
        // 敵船のスポーン距離
        float spawnDistance = DataManager.GetEnemyShipRotateRadius() * 3f;

        // 敵船のスポーン方向
        Vector3 spawnDirection = Quaternion.Euler(0f, spawnAngleY, 0f) * Vector3.forward;

        // 敵船のスポーン位置
        Vector3 spawnPosition = DataManager.GetSubmarinePosition() + spawnDirection * spawnDistance;

        GameObject enemyShip = Instantiate(enemyShipPrefab, spawnPosition, spawnRotation);
        enemyShip.name = "EnemyShip_" + (++enemyShipCount);

        if (enemyShip == null)
        {
            Debug.LogError("Failed to instantiate enemy ship.");
            return false;
        }

        return true;
    }
}
