using UnityEngine;

public class TutorialSceneGameManager : MonoBehaviour
{
    [Header("センサ")]
    [SerializeField, Tooltip("センサ値を取得するSensorRead")]
    private SensorRead sensorRead;

    [Header("潜望鏡")]
    [SerializeField, Tooltip("潜望鏡の親オブジェクト")]
    private Transform periscopeRoot;

    [Header("チュートリアル用の敵船")]
    [SerializeField, Tooltip("シーンに直接配置した敵船")]
    private GameObject tutorialEnemyShip;

    [SerializeField, Tooltip("ゲーム開始時に敵船を潜望鏡の後方へ配置する")]
    private bool placeEnemyBehindPeriscope = true;

    [SerializeField, Tooltip("潜望鏡から敵船までの距離")]
    private float enemyDistance = 40f;

    [SerializeField, Tooltip("敵船のY座標")]
    private float enemyPositionY = 2.2f;

    [SerializeField, Tooltip("敵船モデルの向きの補正")]
    private float enemyRotationOffset = 90f;

    private string registeredEnemyName;

    private void Awake()
    {
        // 前のシーンや前回のプレイで残った敵船一覧を初期化
        DataManager.Initialize();
    }

    private void Start()
    {
        if (sensorRead == null)
        {
            sensorRead = FindFirstObjectByType<SensorRead>();
        }

        if (sensorRead != null)
        {
            DataManager.SetSensorYaw(
                sensorRead.GetYaw()
            );
        }
        else
        {
            Debug.LogWarning(
                "SensorReadが見つかりません。"
            );
        }

        SetupTutorialEnemy();
    }

    private void Update()
    {
        if (sensorRead == null)
        {
            return;
        }

        // Submarine.csが使用するヨー角を更新
        DataManager.SetSensorYaw(
            sensorRead.GetYaw()
        );
    }

    private void SetupTutorialEnemy()
    {
        if (tutorialEnemyShip == null)
        {
            Debug.LogError(
                "チュートリアル用の敵船が設定されていません。"
            );
            return;
        }

        // DataManagerへ登録できる名前にする
        registeredEnemyName = "EnemyShip_1";
        tutorialEnemyShip.name = registeredEnemyName;

        if (placeEnemyBehindPeriscope)
        {
            PlaceEnemyBehindPeriscope();
        }

        bool registered =
            DataManager.AddEnemyShip(
                registeredEnemyName
            );

        if (!registered)
        {
            Debug.LogError(
                "敵船をDataManagerへ登録できませんでした。"
            );
        }
    }

    private void PlaceEnemyBehindPeriscope()
    {
        if (periscopeRoot == null)
        {
            Debug.LogError(
                "Periscope Rootが設定されていません。"
            );
            return;
        }

        // 潜望鏡の初期正面と反対方向
        Vector3 behindDirection =
            -periscopeRoot.forward;

        Vector3 enemyPosition =
            periscopeRoot.position +
            behindDirection * enemyDistance;

        enemyPosition.y = enemyPositionY;

        tutorialEnemyShip.transform.position =
            enemyPosition;

        // 潜望鏡の方向を向かせる
        Vector3 directionToPeriscope =
            periscopeRoot.position -
            tutorialEnemyShip.transform.position;

        directionToPeriscope.y = 0f;

        if (directionToPeriscope != Vector3.zero)
        {
            Quaternion lookRotation =
                Quaternion.LookRotation(
                    directionToPeriscope.normalized
                );

            tutorialEnemyShip.transform.rotation =
                lookRotation *
                Quaternion.Euler(
                    0f,
                    enemyRotationOffset,
                    0f
                );
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(
            registeredEnemyName))
        {
            DataManager.DeleteEnemyShip(
                registeredEnemyName
            );
        }
    }
}