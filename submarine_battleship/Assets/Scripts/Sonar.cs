using System.Collections.Generic;
using UnityEngine;

public class Sonar : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultSonarInterval =
        1.0f;

    private const float DefaultSonarSearchRadius =
        200.0f;

    private const float DefaultEdgePadding =
        6.0f;

    private const float MinimumPositiveValue =
        0.001f;

    private const float CenterAnchor =
        0.5f;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField, Tooltip(
        "ソナー範囲を示す円のRectTransform")]
    private RectTransform sonarArea;


    [SerializeField, Tooltip(
        "全ての海上接触に共通で使用するソナーポイントPrefab")]
    private GameObject sonarPointPrefab;


    // ============================================================
    // Sonar
    // ============================================================

    [Header("Sonar")]

    [SerializeField, Tooltip(
        "ソナー情報を更新する間隔")]
    [Min(MinimumPositiveValue)]
    private float sonarInterval =
        DefaultSonarInterval;


    [SerializeField, Tooltip(
        "ソナーで探知可能な最大距離")]
    [Min(MinimumPositiveValue)]
    private float sonarSearchRadius =
        DefaultSonarSearchRadius;


    [SerializeField, Tooltip(
        "ソナー円の端から内側へ確保する余白")]
    [Min(0.0f)]
    private float edgePadding =
        DefaultEdgePadding;


    [SerializeField, Tooltip(
        "潜水艦の向きを基準として表示を回転する")]
    private bool rotateWithSubmarine =
        true;


    [SerializeField, Tooltip(
        "ソナー表示方向の補正角")]
    private float rotationOffsetDegrees =
        0.0f;


    // ============================================================
    // Compatibility
    // ============================================================

    [Header("Compatibility")]

    [SerializeField, Tooltip(
        "SurfaceContactが存在しない場合、" +
        "従来のDataManager敵艦リストを使用する。" +
        "チュートリアル互換用")]
    private bool useLegacyEnemyListWhenNoContacts =
        true;


    // ============================================================
    // 内部
    // ============================================================

    private float timeAccumulator =
        0.0f;


    private readonly List<GameObject>
        generatedPoints =
            new List<GameObject>();


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ClearSonarPoints();
    }


    // ============================================================
    // Enable
    // ============================================================

    private void OnEnable()
    {
        // 開いた瞬間に更新
        timeAccumulator =
            sonarInterval;
    }


    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        ClearSonarPoints();
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (
            sonarArea == null ||
            sonarPointPrefab == null
        )
        {
            return;
        }


        timeAccumulator +=
            Time.deltaTime;


        if (
            timeAccumulator <
            sonarInterval
        )
        {
            return;
        }


        timeAccumulator =
            0.0f;


        UpdateSonar();
    }


    // ============================================================
    // Sonar更新
    // ============================================================

    private void UpdateSonar()
    {
        ClearSonarPoints();


        IReadOnlyList<SurfaceContact>
            contacts =
                SurfaceContact
                    .GetRegisteredContacts();


        int validSurfaceContactCount =
            0;


        for (
            int index = 0;
            index < contacts.Count;
            index++
        )
        {
            SurfaceContact contact =
                contacts[index];


            if (
                contact == null ||
                !contact.isActiveAndEnabled ||
                !contact.GetIsSonarDetectable()
            )
            {
                continue;
            }


            validSurfaceContactCount++;


            TryGenerateContactPoint(
                contact.GetWorldPosition()
            );
        }


        // ========================================================
        // Tutorial等、SurfaceContact未導入シーンとの互換
        // ========================================================

        if (
            validSurfaceContactCount <= 0 &&
            useLegacyEnemyListWhenNoContacts
        )
        {
            GenerateLegacyEnemyPoints();
        }
    }


    // ============================================================
    // 1接触を表示
    // ============================================================

    private void TryGenerateContactPoint(
        Vector3 contactWorldPosition
    )
    {
        Vector3 submarinePosition =
            DataManager
                .GetSubmarinePosition();


        Vector3 direction3D =
            contactWorldPosition -
            submarinePosition;


        direction3D.y =
            0.0f;


        float distance =
            direction3D.magnitude;


        if (
            distance >
            sonarSearchRadius
        )
        {
            return;
        }


        Vector2 direction =
            new Vector2(
                direction3D.x,
                direction3D.z
            );


        Vector2 normalizedDirection =
            direction.sqrMagnitude >
            Mathf.Epsilon
                ? direction.normalized
                : Vector2.zero;


        float distanceRate =
            Mathf.Clamp01(
                distance /
                sonarSearchRadius
            );


        float displayRadius =
            CalculateDisplayRadius();


        Vector2 displayPosition =
            normalizedDirection *
            distanceRate *
            displayRadius;


        if (rotateWithSubmarine)
        {
            float rotation =
                DataManager
                    .GetSubmarineRotation()
                +
                rotationOffsetDegrees;


            displayPosition =
                RotateVector2(
                    displayPosition,
                    rotation
                );
        }


        GenerateSonarPoint(
            displayPosition
        );
    }


    // ============================================================
    // 従来Enemyリスト
    // ============================================================

    private void GenerateLegacyEnemyPoints()
    {
        List<float[]> rawList =
            DataManager
                .GetEnemyShipDistanceList();


        if (rawList == null)
        {
            return;
        }


        float displayRadius =
            CalculateDisplayRadius();


        for (
            int index = 0;
            index < rawList.Count;
            index++
        )
        {
            float[] data =
                rawList[index];


            if (
                data == null ||
                data.Length < 3
            )
            {
                continue;
            }


            float directionX =
                data[0];

            float directionZ =
                data[1];

            float distance =
                data[2];


            if (
                distance >
                sonarSearchRadius
            )
            {
                continue;
            }


            Vector2 direction =
                new Vector2(
                    directionX,
                    directionZ
                );


            Vector2 normalizedDirection =
                direction.sqrMagnitude >
                Mathf.Epsilon
                    ? direction.normalized
                    : Vector2.zero;


            float distanceRate =
                Mathf.Clamp01(
                    distance /
                    sonarSearchRadius
                );


            Vector2 displayPosition =
                normalizedDirection *
                distanceRate *
                displayRadius;


            if (rotateWithSubmarine)
            {
                float rotation =
                    DataManager
                        .GetSubmarineRotation()
                    +
                    rotationOffsetDegrees;


                displayPosition =
                    RotateVector2(
                        displayPosition,
                        rotation
                    );
            }


            GenerateSonarPoint(
                displayPosition
            );
        }
    }


    // ============================================================
    // 表示可能半径
    // ============================================================

    private float CalculateDisplayRadius()
    {
        float areaRadius =
            Mathf.Min(
                sonarArea.rect.width,
                sonarArea.rect.height
            )
            *
            CenterAnchor;


        float pointRadius =
            0.0f;


        RectTransform prefabRect =
            sonarPointPrefab
                .GetComponent<
                    RectTransform
                >();


        if (prefabRect != null)
        {
            pointRadius =
                Mathf.Max(
                    prefabRect.rect.width,
                    prefabRect.rect.height
                )
                *
                CenterAnchor;
        }


        return
            Mathf.Max(
                0.0f,
                areaRadius -
                pointRadius -
                edgePadding
            );
    }


    // ============================================================
    // Point生成
    // ============================================================

    private void GenerateSonarPoint(
        Vector2 anchoredPosition
    )
    {
        GameObject point =
            Instantiate(
                sonarPointPrefab,
                sonarArea
            );


        RectTransform pointRect =
            point.GetComponent<
                RectTransform
            >();


        if (pointRect != null)
        {
            Vector2 center =
                new Vector2(
                    CenterAnchor,
                    CenterAnchor
                );


            pointRect.anchorMin =
                center;

            pointRect.anchorMax =
                center;

            pointRect.pivot =
                center;

            pointRect.anchoredPosition =
                anchoredPosition;
        }


        point.SetActive(
            true
        );


        generatedPoints.Add(
            point
        );
    }


    // ============================================================
    // Point削除
    // ============================================================

    private void ClearSonarPoints()
    {
        for (
            int index = 0;
            index < generatedPoints.Count;
            index++
        )
        {
            if (
                generatedPoints[index] !=
                null
            )
            {
                Destroy(
                    generatedPoints[index]
                );
            }
        }


        generatedPoints.Clear();
    }


    // ============================================================
    // Vector回転
    // ============================================================

    private Vector2 RotateVector2(
        Vector2 vector,
        float angleDegree
    )
    {
        float radian =
            angleDegree *
            Mathf.Deg2Rad;


        float sin =
            Mathf.Sin(
                radian
            );


        float cos =
            Mathf.Cos(
                radian
            );


        float x =
            vector.x *
            cos
            -
            vector.y *
            sin;


        float y =
            vector.x *
            sin
            +
            vector.y *
            cos;


        return
            new Vector2(
                x,
                y
            );
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        sonarInterval =
            Mathf.Max(
                MinimumPositiveValue,
                sonarInterval
            );


        sonarSearchRadius =
            Mathf.Max(
                MinimumPositiveValue,
                sonarSearchRadius
            );


        edgePadding =
            Mathf.Max(
                0.0f,
                edgePadding
            );
    }
}