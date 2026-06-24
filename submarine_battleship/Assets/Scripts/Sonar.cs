using UnityEngine;
using System.Collections.Generic;

public class Sonar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField, Tooltip("ソナー範囲を示す円のRectTransform")] private RectTransform sonarArea;

    [SerializeField, Tooltip("ソナー上に表示する敵船ポイントのUIプレハブ")] private GameObject sonarPointPrefab;

    private float sonarInterval = 1f;           // ソナー情報を更新する間隔
    private float sonarSearchRadius = 200f;     // ソナーで探知できる距離（単位はゲーム内の距離単位）
    private float edgePadding = 6f;             // ソナー円の端から少し内側に表示する余白
    private bool rotateWithSubmarine = true;    // 潜水艦の向きに合わせてソナー表示を回転させるかどうか

    private float timeAccumulator = 0f;

    private readonly List<GameObject> generatedPoints = new();

    void Start()
    {
        ClearSonarPoints();
    }

    void OnEnable()
    {
        // ソナー画面を開いた瞬間にすぐ更新する
        timeAccumulator = sonarInterval;
    }

    void OnDisable()
    {
        ClearSonarPoints();
    }

    void Update()
    {
        if (sonarArea == null) return;
        if (sonarPointPrefab == null) return;

        timeAccumulator += Time.deltaTime;

        if (timeAccumulator < sonarInterval) return;

        timeAccumulator = 0f;

        UpdateSonar();
    }

    private void UpdateSonar()
    {
        ClearSonarPoints();

        List<float[]> rawList = DataManager.GetEnemyShipDistanceList();

        if (rawList == null) return;

        // SonarAreaの半径を求める
        float areaRadius = Mathf.Min(sonarArea.rect.width, sonarArea.rect.height) * 0.5f;

        float pointRadius = 0f;
        RectTransform prefabRect = sonarPointPrefab.GetComponent<RectTransform>();

        if (prefabRect != null)
        {
            pointRadius = Mathf.Max(prefabRect.rect.width, prefabRect.rect.height) * 0.5f;
        }

        float displayRadius = areaRadius - pointRadius - edgePadding;

        for (int i = 0; i < rawList.Count; i++)
        {
            float directionX = rawList[i][0];
            float directionZ = rawList[i][1];
            float distance = rawList[i][2];

            // 探知範囲外なら表示しない
            if (distance > sonarSearchRadius) continue;

            Vector2 direction = new Vector2(directionX, directionZ).normalized;

            // 距離を0〜1に変換
            float distanceRate = distance / sonarSearchRadius;

            // ソナー円の中での表示位置
            Vector2 displayPosition = direction * distanceRate * displayRadius;

            // 潜水艦の向きに合わせて回転
            if (rotateWithSubmarine)
            {
                displayPosition = RotateVector2(displayPosition, DataManager.GetSubmarineRotation());
            }

            GenerateSonarPoint(displayPosition);
        }
    }

    private void GenerateSonarPoint(Vector2 anchoredPosition)
    {
        GameObject point = Instantiate(sonarPointPrefab, sonarArea);

        RectTransform pointRect = point.GetComponent<RectTransform>();

        if (pointRect != null)
        {
            pointRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointRect.pivot = new Vector2(0.5f, 0.5f);
            pointRect.anchoredPosition = anchoredPosition;
        }

        point.SetActive(true);
        generatedPoints.Add(point);
    }

    private void ClearSonarPoints()
    {
        for (int i = 0; i < generatedPoints.Count; i++)
        {
            if (generatedPoints[i] != null)
            {
                Destroy(generatedPoints[i]);
            }
        }

        generatedPoints.Clear();
    }

    private Vector2 RotateVector2(Vector2 vector, float angleDegree)
    {
        float rad = angleDegree * Mathf.Deg2Rad;

        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        float x = vector.x * cos - vector.y * sin;
        float y = vector.x * sin + vector.y * cos;

        return new Vector2(x, y);
    }
}