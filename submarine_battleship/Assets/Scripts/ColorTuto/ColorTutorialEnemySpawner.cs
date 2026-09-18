using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public class ColorTutorialEnemySpawner : MonoBehaviour
{
    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "チュートリアルで使用する敵艦Prefab。" +
        "yukikaze_ColorMemoryを複製したTutorial用Prefab推奨")]
    private GameObject tutorialEnemyPrefab;

    [SerializeField, Tooltip(
        "潜水艦。未設定の場合はSubmarineを自動検索する")]
    private Transform submarineTransform;

    // ============================================================
    // Spawn Position
    // ============================================================

    [Header("Spawn Position")]

    [SerializeField, Min(1.0f), Tooltip(
        "潜水艦から敵艦までの水平距離")]
    private float spawnDistance =
        40.0f;

    [SerializeField, Tooltip(
        "潜水艦の正面を0度としたスポーン角度。" +
        "負の値で左側、正の値で右側")]
    private float spawnAngleOffsetDegrees =
        -35.0f;

    [SerializeField, Tooltip(
        "敵艦を配置するWorld Y")]
    private float spawnWorldY =
        0.0f;

    [SerializeField, Tooltip(
        "敵艦モデルのY回転補正。" +
        "船体の向きが不自然な場合に調整する")]
    private float enemyHeadingOffsetDegrees =
        90.0f;

    // ============================================================
    // Tutorial Behavior
    // ============================================================

    [Header("Tutorial Behavior")]

    [SerializeField, Tooltip(
        "シーン開始時に自動で1隻生成する")]
    private bool spawnOnStart =
        true;

    [SerializeField, Tooltip(
        "チュートリアル中は潜水艦の自動前進を停止する。" +
        "敵艦との位置関係を一定に保つためON推奨")]
    private bool stopSubmarineAutomaticMovement =
        true;

    [SerializeField, Tooltip(
        "PrefabにColorMemoryEnemyShipがある場合、" +
        "本編用の自動移動・ランダム信号開始を停止する")]
    private bool disableColorMemoryEnemyShip =
        true;

    [SerializeField, Tooltip(
        "生成した敵艦のRigidbodyを固定する")]
    private bool freezeEnemyRigidbody =
        true;

    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;

    [SerializeField]
    private GameObject spawnedEnemy;

    // ============================================================
    // Internal
    // ============================================================

    private Submarine submarine;

    private bool previousAutomaticMovementEnabled =
        true;

    private bool automaticMovementStateStored =
        false;

    // ============================================================
    // Start
    // ============================================================

    private IEnumerator Start()
    {
        ResolveSubmarine();

        StopSubmarineIfNeeded();

        if (!spawnOnStart)
        {
            yield break;
        }

        // Submarine.Start / DataManager初期化を待つ。
        yield return null;

        SpawnTutorialEnemy();
    }

    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        RestoreSubmarineMovement();
    }

    // ============================================================
    // Resolve
    // ============================================================

    private void ResolveSubmarine()
    {
        if (submarineTransform != null)
        {
            submarine =
                submarineTransform
                    .GetComponent<Submarine>();

            if (submarine == null)
            {
                submarine =
                    submarineTransform
                        .GetComponentInParent<Submarine>();
            }

            return;
        }

        submarine =
            FindFirstObjectByType<Submarine>();

        if (submarine != null)
        {
            submarineTransform =
                submarine.transform;
        }
    }

    // ============================================================
    // Submarine
    // ============================================================

    private void StopSubmarineIfNeeded()
    {
        if (
            !stopSubmarineAutomaticMovement ||
            submarine == null
        )
        {
            return;
        }

        previousAutomaticMovementEnabled =
            submarine
                .GetAutomaticForwardMovementEnabled();

        automaticMovementStateStored =
            true;

        submarine
            .SetAutomaticForwardMovementEnabled(
                false
            );
    }

    private void RestoreSubmarineMovement()
    {
        if (
            !automaticMovementStateStored ||
            submarine == null
        )
        {
            return;
        }

        submarine
            .SetAutomaticForwardMovementEnabled(
                previousAutomaticMovementEnabled
            );

        automaticMovementStateStored =
            false;
    }

    // ============================================================
    // Spawn
    // ============================================================

    public GameObject SpawnTutorialEnemy()
    {
        if (spawnedEnemy != null)
        {
            return
                spawnedEnemy;
        }

        if (tutorialEnemyPrefab == null)
        {
            Debug.LogError(
                "ColorTutorialEnemySpawner: " +
                "Tutorial Enemy Prefabが設定されていません。"
            );

            return null;
        }

        ResolveSubmarine();

        Vector3 origin;
        float baseYaw;

        if (submarineTransform != null)
        {
            origin =
                submarineTransform.position;

            baseYaw =
                submarineTransform
                    .eulerAngles
                    .y;
        }
        else
        {
            origin =
                DataManager
                    .GetSubmarinePosition();

            baseYaw =
                DataManager
                    .GetSubmarineRotation();
        }

        float spawnYaw =
            baseYaw +
            spawnAngleOffsetDegrees;

        Vector3 direction =
            Quaternion.Euler(
                0.0f,
                spawnYaw,
                0.0f
            )
            *
            Vector3.forward;

        Vector3 spawnPosition =
            origin +
            direction.normalized *
            spawnDistance;

        spawnPosition.y =
            spawnWorldY;

        Quaternion spawnRotation =
            Quaternion.Euler(
                0.0f,
                spawnYaw +
                enemyHeadingOffsetDegrees,
                0.0f
            );

        spawnedEnemy =
            Instantiate(
                tutorialEnemyPrefab,
                spawnPosition,
                spawnRotation
            );

        spawnedEnemy.name =
            "TutorialEnemy";

        ConfigureSpawnedEnemy(
            spawnedEnemy
        );

        if (debugLog)
        {
            Debug.Log(
                "ColorTutorialEnemySpawner: 敵艦生成 / " +
                "Position = " +
                spawnPosition +
                " / SpawnYaw = " +
                spawnYaw
            );
        }

        return
            spawnedEnemy;
    }

    // ============================================================
    // Configure
    // ============================================================

    private void ConfigureSpawnedEnemy(
        GameObject enemy
    )
    {
        if (enemy == null)
        {
            return;
        }

        if (disableColorMemoryEnemyShip)
        {
            ColorMemoryEnemyShip colorMemoryEnemy =
                enemy
                    .GetComponent<ColorMemoryEnemyShip>();

            if (colorMemoryEnemy != null)
            {
                colorMemoryEnemy.enabled =
                    false;
            }
        }

        SurfaceContact surfaceContact =
            enemy
                .GetComponent<SurfaceContact>();

        if (surfaceContact == null)
        {
            surfaceContact =
                enemy
                    .AddComponent<SurfaceContact>();
        }

        surfaceContact
            .SetContactType(
                SurfaceContactType.Enemy
            );

        surfaceContact
            .SetSonarDetectable(
                true
            );

        if (freezeEnemyRigidbody)
        {
            Rigidbody body =
                enemy
                    .GetComponent<Rigidbody>();

            if (body != null)
            {
                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;

                body.useGravity =
                    false;

                body.isKinematic =
                    true;
            }
        }
    }

    // ============================================================
    // Public
    // ============================================================

    public GameObject GetSpawnedEnemy()
    {
        return
            spawnedEnemy;
    }

    public void DespawnTutorialEnemy()
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        Destroy(
            spawnedEnemy
        );

        spawnedEnemy =
            null;
    }

    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        spawnDistance =
            Mathf.Max(
                1.0f,
                spawnDistance
            );
    }
}
