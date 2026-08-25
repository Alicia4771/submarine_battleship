using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ================================================================
// 1種類の海上接触に対するスポーン設定
// ================================================================

[System.Serializable]
public class AmbientContactSpawnRule
{
    // ============================================================
    // 定数
    // ============================================================

    private const int DefaultInitialCount =
        1;

    private const int MinimumContactCount =
        0;

    private const float DefaultMinimumSpawnDistance =
        50.0f;

    private const float DefaultMaximumSpawnDistance =
        120.0f;

    private const float DefaultMinimumSpawnAngle =
        0.0f;

    private const float DefaultMaximumSpawnAngle =
        360.0f;

    private const float DefaultWorldY =
        0.0f;

    private const float MinimumNonNegativeValue =
        0.0f;


    // ============================================================
    // 種類
    // ============================================================

    [Header("Contact")]

    [SerializeField, Tooltip(
        "このルールで生成する船の種類。" +
        "FriendlyまたはNeutralを指定する")]
    private SurfaceContactType contactType =
        SurfaceContactType.Friendly;


    // ============================================================
    // Prefab
    // ============================================================

    [SerializeField, Tooltip(
        "この種類で使用できるPrefab。" +
        "複数登録した場合はランダムに選択する")]
    private GameObject[] prefabs =
        new GameObject[0];


    // ============================================================
    // 数
    // ============================================================

    [SerializeField, Tooltip(
        "1ラウンドにつき生成する船の数")]
    [Min(MinimumContactCount)]
    private int initialCount =
        DefaultInitialCount;


    // ============================================================
    // 距離
    // ============================================================

    [Header("Spawn Distance")]

    [SerializeField, Tooltip(
        "潜水艦からの最小スポーン距離")]
    [Min(MinimumNonNegativeValue)]
    private float minimumSpawnDistance =
        DefaultMinimumSpawnDistance;


    [SerializeField, Tooltip(
        "潜水艦からの最大スポーン距離")]
    [Min(MinimumNonNegativeValue)]
    private float maximumSpawnDistance =
        DefaultMaximumSpawnDistance;


    // ============================================================
    // 角度
    // ============================================================

    [Header("Spawn Angle")]

    [SerializeField, Tooltip(
        "潜水艦正面を基準にした最小スポーン角度")]
    private float minimumSpawnAngle =
        DefaultMinimumSpawnAngle;


    [SerializeField, Tooltip(
        "潜水艦正面を基準にした最大スポーン角度")]
    private float maximumSpawnAngle =
        DefaultMaximumSpawnAngle;


    // ============================================================
    // 高さ
    // ============================================================

    [Header("Height")]

    [SerializeField, Tooltip(
        "生成する船のワールドY座標")]
    private float worldY =
        DefaultWorldY;


    // ============================================================
    // Getter
    // ============================================================

    public SurfaceContactType ContactType
    {
        get
        {
            return contactType;
        }
    }


    public GameObject[] Prefabs
    {
        get
        {
            return prefabs;
        }
    }


    public int InitialCount
    {
        get
        {
            return initialCount;
        }
    }


    public float MinimumSpawnDistance
    {
        get
        {
            return minimumSpawnDistance;
        }
    }


    public float MaximumSpawnDistance
    {
        get
        {
            return maximumSpawnDistance;
        }
    }


    public float MinimumSpawnAngle
    {
        get
        {
            return minimumSpawnAngle;
        }
    }


    public float MaximumSpawnAngle
    {
        get
        {
            return maximumSpawnAngle;
        }
    }


    public float WorldY
    {
        get
        {
            return worldY;
        }
    }


    // ============================================================
    // 設定値検証
    // ============================================================

    public void Validate()
    {
        initialCount =
            Mathf.Max(
                MinimumContactCount,
                initialCount
            );


        minimumSpawnDistance =
            Mathf.Max(
                MinimumNonNegativeValue,
                minimumSpawnDistance
            );


        maximumSpawnDistance =
            Mathf.Max(
                minimumSpawnDistance,
                maximumSpawnDistance
            );


        if (
            maximumSpawnAngle <
            minimumSpawnAngle
        )
        {
            float temporaryAngle =
                minimumSpawnAngle;


            minimumSpawnAngle =
                maximumSpawnAngle;


            maximumSpawnAngle =
                temporaryAngle;
        }


        if (
            contactType !=
                SurfaceContactType.Friendly
            &&
            contactType !=
                SurfaceContactType.Neutral
        )
        {
            contactType =
                SurfaceContactType.Friendly;
        }
    }
}


// =================================================================
// AmbientContactSpawner
// =================================================================

[DisallowMultipleComponent]
public class AmbientContactSpawner : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultMinimumSeparation =
        10.0f;

    private const int DefaultMaximumSpawnAttempts =
        20;

    private const int MinimumSpawnAttempts =
        1;

    private const float MinimumNonNegativeValue =
        0.0f;

    private const float DefaultRotationX =
        0.0f;

    private const float DefaultRotationZ =
        0.0f;

    private const string FriendlyShipNamePrefix =
        "FriendlyShip_";

    private const string NeutralShipNamePrefix =
        "NeutralShip_";

    private const string GenericContactName =
        "SurfaceContact";


    // ============================================================
    // スポーン基準
    // ============================================================

    [Header("Spawn Origin")]

    [SerializeField, Tooltip(
        "スポーン位置の基準。" +
        "未設定ならSubmarineを自動検索する")]
    private Transform spawnOrigin;


    // ============================================================
    // スポーンルール
    // ============================================================

    [Header("Spawn Rules")]

    [SerializeField, Tooltip(
        "Friendly・Neutral船のスポーン設定")]
    private List<AmbientContactSpawnRule>
        spawnRules =
            new List<AmbientContactSpawnRule>();


    // ============================================================
    // 配置設定
    // ============================================================

    [Header("Placement")]

    [SerializeField, Tooltip(
        "ゲーム開始時にSpawn Rulesの船を生成する")]
    private bool spawnOnStart =
        true;


    [SerializeField, Tooltip(
        "船同士が近すぎないようにする最低距離")]
    [Min(MinimumNonNegativeValue)]
    private float minimumSeparation =
        DefaultMinimumSeparation;


    [SerializeField, Tooltip(
        "配置可能な位置を探す最大試行回数")]
    [Min(MinimumSpawnAttempts)]
    private int maximumSpawnAttempts =
        DefaultMaximumSpawnAttempts;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "生成・削除情報をConsoleへ表示する")]
    private bool debugLog =
        true;


    // ============================================================
    // 内部状態
    // ============================================================

    private int friendlyShipCount =
        0;

    private int neutralShipCount =
        0;


    // このSpawner自身が生成した
    // Friendly / Neutralだけを管理する
    private readonly List<GameObject>
        spawnedContacts =
            new List<GameObject>();


    // ============================================================
    // Start
    // ============================================================

    private IEnumerator Start()
    {
        ResolveSpawnOrigin();


        if (!spawnOnStart)
        {
            yield break;
        }


        // GameManagerやSubmarineの初期化待ち
        yield return null;


        SpawnInitialContacts();
    }


    // ============================================================
    // Spawn Origin
    // ============================================================

    private void ResolveSpawnOrigin()
    {
        if (spawnOrigin != null)
        {
            return;
        }


        Submarine submarine =
            FindFirstObjectByType<Submarine>();


        if (submarine != null)
        {
            spawnOrigin =
                submarine.transform;


            return;
        }


        if (debugLog)
        {
            Debug.LogWarning(
                "AmbientContactSpawner: " +
                "Submarineが見つかりません。" +
                "DataManagerの潜水艦位置を使用します。"
            );
        }
    }


    // ============================================================
    // 1ラウンド分を生成
    // ============================================================

    public void SpawnInitialContacts()
    {
        CleanupSpawnedContactList();


        if (spawnRules == null)
        {
            return;
        }


        for (
            int ruleIndex = 0;
            ruleIndex < spawnRules.Count;
            ruleIndex++
        )
        {
            AmbientContactSpawnRule rule =
                spawnRules[ruleIndex];


            if (rule == null)
            {
                continue;
            }


            if (!IsAmbientType(
                rule.ContactType
            ))
            {
                Debug.LogWarning(
                    "AmbientContactSpawnerでは" +
                    "FriendlyまたはNeutralのみ生成できます。"
                );


                continue;
            }


            for (
                int contactIndex = 0;
                contactIndex < rule.InitialCount;
                contactIndex++
            )
            {
                TrySpawnContact(
                    rule
                );
            }
        }
    }


    // ============================================================
    // 指定種類を1隻生成
    // ============================================================

    public bool SpawnOne(
        SurfaceContactType contactType
    )
    {
        if (!IsAmbientType(
            contactType
        ))
        {
            return false;
        }


        if (spawnRules == null)
        {
            return false;
        }


        for (
            int ruleIndex = 0;
            ruleIndex < spawnRules.Count;
            ruleIndex++
        )
        {
            AmbientContactSpawnRule rule =
                spawnRules[ruleIndex];


            if (
                rule != null &&
                rule.ContactType ==
                    contactType
            )
            {
                return
                    TrySpawnContact(
                        rule
                    );
            }
        }


        return false;
    }


    // ============================================================
    // 生成済みFriendly / Neutralを全削除
    // ============================================================

    public int ClearSpawnedContacts()
    {
        CleanupSpawnedContactList();


        int removedCount =
            0;


        for (
            int index = spawnedContacts.Count - 1;
            index >= 0;
            index--
        )
        {
            GameObject target =
                spawnedContacts[index];


            if (target == null)
            {
                continue;
            }


            // SurfaceContact.OnDisableを即時発生させ、
            // ソナー登録から先に外す
            target.SetActive(
                false
            );


            Destroy(
                target
            );


            removedCount++;
        }


        spawnedContacts.Clear();


        if (debugLog)
        {
            Debug.Log(
                "Friendly / Neutralをリセットしました。削除数: " +
                removedCount
            );
        }


        return
            removedCount;
    }


    // ============================================================
    // 現在管理している船数
    // ============================================================

    public int GetSpawnedContactCount()
    {
        CleanupSpawnedContactList();


        return
            spawnedContacts.Count;
    }


    // ============================================================
    // List掃除
    // ============================================================

    private void CleanupSpawnedContactList()
    {
        for (
            int index = spawnedContacts.Count - 1;
            index >= 0;
            index--
        )
        {
            if (
                spawnedContacts[index] ==
                null
            )
            {
                spawnedContacts.RemoveAt(
                    index
                );
            }
        }
    }


    // ============================================================
    // 1隻生成
    // ============================================================

    private bool TrySpawnContact(
        AmbientContactSpawnRule rule
    )
    {
        if (rule == null)
        {
            return false;
        }


        GameObject prefab =
            GetRandomPrefab(
                rule
            );


        if (prefab == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    rule.ContactType +
                    "用Prefabが設定されていません。"
                );
            }


            return false;
        }


        // Enemy用Prefabを誤って登録するのを防止
        if (
            prefab.GetComponent<EnemyShip>() !=
            null
        )
        {
            Debug.LogError(
                "AmbientContactSpawnerには" +
                "EnemyShip付きPrefabを設定できません: " +
                prefab.name
            );


            return false;
        }


        if (
            !TryFindSpawnPosition(
                rule,
                out Vector3 spawnPosition
            )
        )
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    rule.ContactType +
                    "の配置可能な場所が見つかりませんでした。"
                );
            }


            return false;
        }


        float spawnYaw =
            Random.Range(
                rule.MinimumSpawnAngle,
                rule.MaximumSpawnAngle
            );


        Quaternion spawnRotation =
            Quaternion.Euler(
                DefaultRotationX,
                spawnYaw,
                DefaultRotationZ
            );


        GameObject spawnedObject =
            Instantiate(
                prefab,
                spawnPosition,
                spawnRotation
            );


        if (spawnedObject == null)
        {
            return false;
        }


        SurfaceContact surfaceContact =
            spawnedObject
                .GetComponent<SurfaceContact>();


        if (surfaceContact == null)
        {
            surfaceContact =
                spawnedObject
                    .AddComponent<SurfaceContact>();
        }


        surfaceContact.SetContactType(
            rule.ContactType
        );


        surfaceContact.SetSonarDetectable(
            true
        );


        PassiveSurfaceShip passiveShip =
            spawnedObject
                .GetComponent<PassiveSurfaceShip>();


        if (passiveShip == null)
        {
            Debug.LogWarning(
                spawnedObject.name +
                " にPassiveSurfaceShipがありません。" +
                "船は生成されますが、自動航行しません。"
            );
        }
        else
        {
            passiveShip.ConfigureContactType(
                rule.ContactType
            );
        }


        AssignContactName(
            spawnedObject,
            rule.ContactType
        );


        // このSpawnerが生成した船として記録
        spawnedContacts.Add(
            spawnedObject
        );


        if (debugLog)
        {
            Debug.Log(
                "海上接触を生成しました: " +
                spawnedObject.name +
                " / Type=" +
                rule.ContactType +
                " / Position=" +
                spawnPosition
            );
        }


        return true;
    }


    // ============================================================
    // Prefab
    // ============================================================

    private GameObject GetRandomPrefab(
        AmbientContactSpawnRule rule
    )
    {
        GameObject[] prefabs =
            rule.Prefabs;


        if (
            prefabs == null ||
            prefabs.Length <= 0
        )
        {
            return null;
        }


        int startIndex =
            Random.Range(
                0,
                prefabs.Length
            );


        for (
            int offset = 0;
            offset < prefabs.Length;
            offset++
        )
        {
            int prefabIndex =
                (
                    startIndex +
                    offset
                )
                %
                prefabs.Length;


            if (
                prefabs[prefabIndex] !=
                null
            )
            {
                return
                    prefabs[prefabIndex];
            }
        }


        return null;
    }


    // ============================================================
    // スポーン位置
    // ============================================================

    private bool TryFindSpawnPosition(
        AmbientContactSpawnRule rule,
        out Vector3 spawnPosition
    )
    {
        spawnPosition =
            Vector3.zero;


        Vector3 originPosition =
            GetSpawnOriginPosition();


        float originYaw =
            GetSpawnOriginYaw();


        for (
            int attemptIndex = 0;
            attemptIndex < maximumSpawnAttempts;
            attemptIndex++
        )
        {
            float relativeAngle =
                Random.Range(
                    rule.MinimumSpawnAngle,
                    rule.MaximumSpawnAngle
                );


            float worldAngle =
                originYaw +
                relativeAngle;


            float distance =
                Random.Range(
                    rule.MinimumSpawnDistance,
                    rule.MaximumSpawnDistance
                );


            Vector3 direction =
                Quaternion.Euler(
                    DefaultRotationX,
                    worldAngle,
                    DefaultRotationZ
                )
                *
                Vector3.forward;


            Vector3 candidatePosition =
                originPosition +
                direction *
                distance;


            candidatePosition.y =
                rule.WorldY;


            if (
                IsPositionAvailable(
                    candidatePosition
                )
            )
            {
                spawnPosition =
                    candidatePosition;


                return true;
            }
        }


        return false;
    }


    // ============================================================
    // 他船との重複判定
    // ============================================================

    private bool IsPositionAvailable(
        Vector3 candidatePosition
    )
    {
        if (
            minimumSeparation <=
            MinimumNonNegativeValue
        )
        {
            return true;
        }


        IReadOnlyList<SurfaceContact>
            registeredContacts =
                SurfaceContact
                    .GetRegisteredContacts();


        Vector2 candidateXZ =
            new Vector2(
                candidatePosition.x,
                candidatePosition.z
            );


        for (
            int contactIndex = 0;
            contactIndex < registeredContacts.Count;
            contactIndex++
        )
        {
            SurfaceContact contact =
                registeredContacts[
                    contactIndex
                ];


            if (contact == null)
            {
                continue;
            }


            Vector3 contactPosition =
                contact.GetWorldPosition();


            Vector2 contactXZ =
                new Vector2(
                    contactPosition.x,
                    contactPosition.z
                );


            float distance =
                Vector2.Distance(
                    candidateXZ,
                    contactXZ
                );


            if (
                distance <
                minimumSeparation
            )
            {
                return false;
            }
        }


        return true;
    }


    // ============================================================
    // 基準位置
    // ============================================================

    private Vector3 GetSpawnOriginPosition()
    {
        if (spawnOrigin != null)
        {
            return
                spawnOrigin.position;
        }


        return
            DataManager
                .GetSubmarinePosition();
    }


    // ============================================================
    // 基準角度
    // ============================================================

    private float GetSpawnOriginYaw()
    {
        if (spawnOrigin != null)
        {
            return
                spawnOrigin.eulerAngles.y;
        }


        return
            DataManager
                .GetSubmarineRotation();
    }


    // ============================================================
    // 名前
    // ============================================================

    private void AssignContactName(
        GameObject target,
        SurfaceContactType contactType
    )
    {
        if (target == null)
        {
            return;
        }


        switch (contactType)
        {
            case SurfaceContactType.Friendly:

                friendlyShipCount++;


                target.name =
                    FriendlyShipNamePrefix +
                    friendlyShipCount;


                break;


            case SurfaceContactType.Neutral:

                neutralShipCount++;


                target.name =
                    NeutralShipNamePrefix +
                    neutralShipCount;


                break;


            default:

                target.name =
                    GenericContactName;


                break;
        }
    }


    // ============================================================
    // Ambient種類判定
    // ============================================================

    private bool IsAmbientType(
        SurfaceContactType contactType
    )
    {
        return
            contactType ==
                SurfaceContactType.Friendly
            ||
            contactType ==
                SurfaceContactType.Neutral;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        minimumSeparation =
            Mathf.Max(
                MinimumNonNegativeValue,
                minimumSeparation
            );


        maximumSpawnAttempts =
            Mathf.Max(
                MinimumSpawnAttempts,
                maximumSpawnAttempts
            );


        if (spawnRules == null)
        {
            return;
        }


        for (
            int ruleIndex = 0;
            ruleIndex < spawnRules.Count;
            ruleIndex++
        )
        {
            AmbientContactSpawnRule rule =
                spawnRules[ruleIndex];


            if (rule != null)
            {
                rule.Validate();
            }
        }
    }
}