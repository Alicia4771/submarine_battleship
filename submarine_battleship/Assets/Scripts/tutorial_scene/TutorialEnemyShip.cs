using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TutorialEnemyShip : Ship
{
    [Header("--- チュートリアル連携 ---")]

    [SerializeField, Tooltip("チュートリアル全体を管理するスクリプト")]
    private TutorialSceneManager tutorialSceneManager;

    [SerializeField, Tooltip("潜望鏡の親オブジェクト")]
    private Transform periscopeRoot;

    [SerializeField, Tooltip(
        "表示・非表示を切り替える敵船の見た目。未設定の場合は最初の子を使用")]
    private GameObject shipVisual;


    [Header("--- 敵船の配置 ---")]

    [SerializeField, Tooltip(
        "開始時に潜望鏡の後方へ自動配置する")]
    private bool placeBehindPeriscope = true;

    [SerializeField, Tooltip(
        "潜望鏡から敵船までの距離")]
    [Min(1f)]
    private float distanceFromPeriscope = 40f;

    [SerializeField, Tooltip(
        "敵船を配置するY座標")]
    private float enemyPositionY = 2.2f;

    [SerializeField, Tooltip(
        "敵船モデルの向きを補正する角度")]
    private float modelRotationOffset = 90f;

    [SerializeField, Tooltip(
        "DataManagerへ登録するときの名前")]
    private string registeredEnemyName = "EnemyShip_1";


    [Header("--- 潜望鏡の索敵設定 ---")]

    [SerializeField, Tooltip(
        "敵船を発見できる潜望鏡の視野角")]
    [Range(1f, 179f)]
    private float periscopeFOV = 45f;

    [SerializeField, Tooltip(
        "敵船を発見できる最大距離")]
    [Min(0.1f)]
    private float maxDetectDistance = 50f;

    [SerializeField, Tooltip(
        "発見されるまでは敵船モデルを非表示にする")]
    private bool hideUntilDetected = true;


    [Header("--- 発光信号の設定 ---")]

    [SerializeField, Tooltip(
        "自動生成するライトのローカル座標")]
    private Vector3 signalLightLocalPosition =
        new Vector3(0f, 3f, 0f);

    [SerializeField, Tooltip("発光色")]
    private Color signalColor = Color.red;

    [SerializeField, Tooltip("ライトの明るさ")]
    private float signalIntensity = 1500f;

    [SerializeField, Tooltip("ライトの届く範囲")]
    private float signalRange = 150f;

    [SerializeField, Tooltip(
        "同じ信号を繰り返す回数")]
    [Min(1)]
    private int signalRepeatCount = 1;

    [SerializeField, Tooltip(
        "信号を繰り返す場合の待ち時間")]
    [Min(0f)]
    private float signalRepeatInterval = 1.5f;


    private Rigidbody shipRigidbody;
    private Light signalLight;

    private Coroutine signalCoroutine;

    private bool detectionEnabled = false;
    private bool isDetected = false;
    private bool signalStarted = false;
    private bool signalFinished = false;
    private bool isRegistered = false;


    protected override void Start()
    {
        base.Start();

        FindReferences();
        SetupShipVisual();

        if (placeBehindPeriscope)
        {
            PlaceBehindPeriscope();
        }

        RegisterEnemyShip();
        CreateAutomaticLight();
        ConfigureRigidbody();

        // 敵船捜索の説明が終わるまでは発見させない
        SetDetectionEnabled(false);
    }

    protected override void Update()
    {
        base.Update();

        if (!detectionEnabled || isDetected)
        {
            return;
        }

        CheckPeriscopeDetection();
    }

    protected override void FixedUpdate()
    {
        // チュートリアルの敵船は移動させない
        if (shipRigidbody == null)
        {
            return;
        }

        shipRigidbody.linearVelocity = Vector3.zero;
        shipRigidbody.angularVelocity = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (signalCoroutine != null)
        {
            StopCoroutine(signalCoroutine);
            signalCoroutine = null;
        }

        if (signalLight != null)
        {
            signalLight.enabled = false;
        }

        if (isRegistered)
        {
            DataManager.DeleteEnemyShip(
                registeredEnemyName
            );

            isRegistered = false;
        }
    }


    // =========================================================
    // 初期設定
    // =========================================================

    private void FindReferences()
    {
        if (tutorialSceneManager == null)
        {
            tutorialSceneManager =
                FindFirstObjectByType<TutorialSceneManager>();
        }

        if (periscopeRoot == null)
        {
            Submarine submarine =
                FindFirstObjectByType<Submarine>();

            if (submarine != null)
            {
                periscopeRoot =
                    submarine.transform;
            }
        }

        shipRigidbody =
            GetComponent<Rigidbody>();
    }

    private void SetupShipVisual()
    {
        if (shipVisual == null &&
            transform.childCount > 0)
        {
            shipVisual =
                transform.GetChild(0).gameObject;
        }

        if (shipVisual != null)
        {
            shipVisual.SetActive(
                !hideUntilDetected
            );
        }
    }

    private void ConfigureRigidbody()
    {
        if (shipRigidbody == null)
        {
            return;
        }

        shipRigidbody.useGravity = false;

        shipRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        // チュートリアルでは完全に固定する
        shipRigidbody.constraints =
            RigidbodyConstraints.FreezeAll;
    }

    private void RegisterEnemyShip()
    {
        if (string.IsNullOrWhiteSpace(
            registeredEnemyName))
        {
            registeredEnemyName =
                "EnemyShip_1";
        }

        gameObject.name =
            registeredEnemyName;

        isRegistered =
            DataManager.AddEnemyShip(
                registeredEnemyName
            );

        if (!isRegistered)
        {
            Debug.LogError(
                "チュートリアルの敵船を" +
                "DataManagerへ登録できませんでした。"
            );
        }
    }


    // =========================================================
    // 敵船の配置
    // =========================================================

    private void PlaceBehindPeriscope()
    {
        if (periscopeRoot == null)
        {
            Debug.LogWarning(
                "Periscope Rootが設定されていないため、" +
                "敵船の自動配置を行いません。"
            );

            return;
        }

        // 潜望鏡の正面とは反対の方向
        Vector3 behindDirection =
            -periscopeRoot.forward;

        behindDirection.y = 0f;

        if (behindDirection.sqrMagnitude <
            0.0001f)
        {
            behindDirection =
                Vector3.back;
        }

        behindDirection.Normalize();

        Vector3 enemyPosition =
            periscopeRoot.position +
            behindDirection *
            distanceFromPeriscope;

        enemyPosition.y =
            enemyPositionY;

        transform.position =
            enemyPosition;

        // 敵船を潜望鏡側へ向ける
        Vector3 directionToPeriscope =
            periscopeRoot.position -
            transform.position;

        directionToPeriscope.y = 0f;

        if (directionToPeriscope.sqrMagnitude >
            0.0001f)
        {
            Quaternion lookRotation =
                Quaternion.LookRotation(
                    directionToPeriscope.normalized
                );

            transform.rotation =
                lookRotation *
                Quaternion.Euler(
                    0f,
                    modelRotationOffset,
                    0f
                );
        }
    }


    // =========================================================
    // 敵船発見
    // =========================================================

    private void CheckPeriscopeDetection()
    {
        Vector3 periscopePosition;
        Vector3 periscopeForward;

        if (periscopeRoot != null)
        {
            periscopePosition =
                periscopeRoot.position;

            periscopeForward =
                periscopeRoot.forward;
        }
        else
        {
            periscopePosition =
                DataManager.GetSubmarinePosition();

            periscopeForward =
                Quaternion.Euler(
                    0f,
                    DataManager.GetSubmarineRotation(),
                    0f
                ) * Vector3.forward;
        }

        Vector3 directionToEnemy =
            transform.position -
            periscopePosition;

        directionToEnemy.y = 0f;
        periscopeForward.y = 0f;

        float distance =
            directionToEnemy.magnitude;

        if (distance >
            maxDetectDistance)
        {
            return;
        }

        if (directionToEnemy.sqrMagnitude <
            0.0001f ||
            periscopeForward.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        float angle =
            Vector3.Angle(
                periscopeForward.normalized,
                directionToEnemy.normalized
            );

        if (angle <= periscopeFOV * 0.5f)
        {
            CompleteDetection();
        }
    }

    private void CompleteDetection()
    {
        if (isDetected)
        {
            return;
        }

        isDetected = true;
        detectionEnabled = false;

        if (shipVisual != null)
        {
            shipVisual.SetActive(true);
        }

        if (tutorialSceneManager != null)
        {
            tutorialSceneManager.NotifyEnemyFound();
        }
    }

    public void SetDetectionEnabled(bool enabled)
    {
        detectionEnabled =
            enabled && !isDetected;
    }

    public bool GetIsDetected()
    {
        return isDetected;
    }

    /// <summary>
    /// デバッグ用に敵船を強制発見する。
    /// </summary>
    public void ForceDetectForDebug()
    {
        CompleteDetection();
    }


    // =========================================================
    // 発光信号
    // =========================================================

    private void CreateAutomaticLight()
    {
        GameObject lightObject =
            new GameObject("AutoSignalLight");

        lightObject.transform.SetParent(
            transform,
            false
        );

        lightObject.transform.localPosition =
            signalLightLocalPosition;

        signalLight =
            lightObject.AddComponent<Light>();

        signalLight.type =
            LightType.Point;

        signalLight.color =
            signalColor;

        signalLight.intensity =
            signalIntensity;

        signalLight.range =
            signalRange;

        signalLight.enabled = false;
    }

    /// <summary>
    /// 司令官の発光説明が終わった後に呼び出す。
    /// </summary>
    public void StartSignal()
    {
        if (!isDetected)
        {
            Debug.LogWarning(
                "敵船がまだ発見されていないため、" +
                "発光信号を開始できません。"
            );

            return;
        }

        if (signalStarted || signalFinished)
        {
            return;
        }

        signalStarted = true;

        signalCoroutine =
            StartCoroutine(
                FlashSignalRoutine()
            );
    }

    private IEnumerator FlashSignalRoutine()
    {
        for (int repeat = 0;
             repeat < signalRepeatCount;
             repeat++)
        {
            // 短・短・長・短
            yield return PlayFlash(
                0.15f,
                0.15f
            );

            yield return PlayFlash(
                0.15f,
                0.15f
            );

            yield return PlayFlash(
                0.65f,
                0.15f
            );

            yield return PlayFlash(
                0.15f,
                1.5f
            );

            if (repeat <
                signalRepeatCount - 1)
            {
                yield return
                    new WaitForSecondsRealtime(
                        signalRepeatInterval
                    );
            }
        }

        signalCoroutine = null;

        FinishSignal();
    }

    private IEnumerator PlayFlash(
        float lightDuration,
        float blankDuration)
    {
        if (signalLight == null)
        {
            yield break;
        }

        signalLight.enabled = true;

        yield return
            new WaitForSecondsRealtime(
                lightDuration
            );

        signalLight.enabled = false;

        yield return
            new WaitForSecondsRealtime(
                blankDuration
            );
    }

    private void FinishSignal()
    {
        if (signalFinished)
        {
            return;
        }

        signalFinished = true;

        if (signalLight != null)
        {
            signalLight.enabled = false;
        }

        if (tutorialSceneManager != null)
        {
            tutorialSceneManager
                .NotifyEnemySignalFinished();
        }
    }

    /// <summary>
    /// デバッグ用に発光信号を終了扱いにする。
    /// </summary>
    public void ForceFinishSignalForDebug()
    {
        if (signalCoroutine != null)
        {
            StopCoroutine(signalCoroutine);
            signalCoroutine = null;
        }

        signalStarted = true;

        FinishSignal();
    }
}