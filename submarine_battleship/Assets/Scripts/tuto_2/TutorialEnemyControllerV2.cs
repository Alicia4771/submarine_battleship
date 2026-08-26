using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-500)]
[RequireComponent(
    typeof(Rigidbody),
    typeof(SurfaceContact),
    typeof(EnemyShip)
)]
[DisallowMultipleComponent]
public class TutorialEnemyControllerV2 : MonoBehaviour
{
    // ============================================================
    // Events
    // ============================================================

    public event Action EnemyFound;

    public event Action SignalFinished;


    // ============================================================
    // References
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "MainScene用のEnemyShipコンポーネント。" +
        "未設定の場合は自動取得する")]
    private EnemyShip enemyShip;


    [SerializeField, Tooltip(
        "潜望鏡の回転基準となるPeriscopeRoot")]
    private Transform periscopeReference;


    [SerializeField, Tooltip(
        "雪風モデルの見た目Root")]
    private GameObject visualRoot;


    [SerializeField, Tooltip(
        "通信システム")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // Placement
    // ============================================================

    [Header("Placement")]

    [SerializeField, Tooltip(
        "開始時に潜望鏡の初期正面の反対側へ配置する")]
    private bool placeBehindPeriscope =
        true;


    [SerializeField, Tooltip(
        "潜望鏡から雪風までの距離")]
    [Min(1.0f)]
    private float distanceFromPeriscope =
        70.0f;


    [SerializeField, Tooltip(
        "雪風のワールドY座標")]
    private float enemyWorldY =
        1.2f;


    [SerializeField, Tooltip(
        "雪風モデル本来の向きを合わせるためのY軸回転補正")]
    private float modelRotationOffset =
        90.0f;


    [SerializeField, Tooltip(
        "従来のスポーン時の向きから、さらに追加するY軸回転角度")]
    private float additionalSpawnRotationY =
        90.0f;


    // ============================================================
    // Detection
    // ============================================================

    [Header("Detection")]

    [SerializeField, Tooltip(
        "雪風を発見できる視野角。左右合計")]
    [Range(1.0f, 179.0f)]
    private float detectionFOV =
        45.0f;


    [SerializeField, Tooltip(
        "雪風を発見できる最大距離")]
    [Min(0.1f)]
    private float maximumDetectionDistance =
        150.0f;


    [SerializeField, Tooltip(
        "雪風を視界に入れ続ける必要がある時間")]
    [Min(0.0f)]
    private float requiredObservationTime =
        0.5f;


    // ============================================================
    // Signal Pattern
    // ============================================================

    [Header("Tutorial Signal")]

    [SerializeField, Tooltip(
        "チュートリアルで使用する固定信号")]
    private SignalSymbol[] tutorialSignalPattern =
    {
        SignalSymbol.Short,
        SignalSymbol.Short,
        SignalSymbol.Long,
        SignalSymbol.Short
    };


    [SerializeField, Tooltip(
        "同じ信号を何回見せるか")]
    [Min(1)]
    private int repeatCount =
        2;


    // ============================================================
    // Cycle Start
    // ============================================================

    [Header("Cycle Start Marker")]

    [SerializeField]
    private Color startMarkerColor =
        Color.yellow;


    [SerializeField, Min(0.0f)]
    private float startMarkerDuration =
        0.4f;


    [SerializeField, Min(0.0f)]
    private float startMarkerBlankDuration =
        0.3f;


    // ============================================================
    // Signal Colors
    // ============================================================

    [Header("Signal Colors")]

    [SerializeField]
    private Color shortSignalColor =
        Color.red;


    [SerializeField]
    private Color longSignalColor =
        new Color(
            1.0f,
            0.5f,
            0.0f,
            1.0f
        );


    // ============================================================
    // Timing
    // ============================================================

    [Header("Signal Timing")]

    [SerializeField, Min(0.0f)]
    private float shortSignalDuration =
        0.15f;


    [SerializeField, Min(0.0f)]
    private float longSignalDuration =
        0.65f;


    [SerializeField, Min(0.0f)]
    private float symbolBlankDuration =
        0.15f;


    [SerializeField, Min(0.0f)]
    private float repeatInterval =
        1.0f;


    // ============================================================
    // Light
    // ============================================================

    [Header("Signal Light")]

    [SerializeField]
    private Vector3 signalLightLocalPosition =
        new Vector3(
            0.0f,
            3.0f,
            0.0f
        );


    [SerializeField, Min(0.0f)]
    private float signalIntensity =
        1500.0f;


    [SerializeField, Min(0.0f)]
    private float signalRange =
        150.0f;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLog =
        false;


    // ============================================================
    // Internal
    // ============================================================

    private Rigidbody shipRigidbody;

    private SurfaceContact surfaceContact;

    private Light signalLight;

    private Coroutine signalCoroutine;


    private bool detectionEnabled =
        false;

    private bool enemyFound =
        false;

    private float observationTime =
        0.0f;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        enemyShip =
            enemyShip != null
                ? enemyShip
                : GetComponent<EnemyShip>();


        shipRigidbody =
            GetComponent<Rigidbody>();


        surfaceContact =
            GetComponent<SurfaceContact>();


        // ========================================================
        // MainScene用EnemyShipはチュートリアルでは動作させない
        // ========================================================

        if (enemyShip != null)
        {
            enemyShip.enabled =
                false;
        }


        // ========================================================
        // Sonar Contact
        // ========================================================

        if (surfaceContact != null)
        {
            surfaceContact.SetContactType(
                SurfaceContactType.Enemy
            );


            surfaceContact.SetSonarDetectable(
                true
            );
        }


        // ========================================================
        // Rigidbody固定
        // ========================================================

        if (shipRigidbody != null)
        {
            shipRigidbody.useGravity =
                false;


            shipRigidbody.linearVelocity =
                Vector3.zero;


            shipRigidbody.angularVelocity =
                Vector3.zero;


            shipRigidbody.constraints =
                RigidbodyConstraints.FreezeAll;
        }
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        ResolveReferences();


        if (visualRoot != null)
        {
            visualRoot.SetActive(
                true
            );
        }


        if (placeBehindPeriscope)
        {
            PlaceEnemyBehindPeriscope();
        }


        CreateSignalLight();


        SetDetectionEnabled(
            false
        );
    }


    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (
            !detectionEnabled ||
            enemyFound
        )
        {
            return;
        }


        UpdateDetection();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        if (signalCoroutine != null)
        {
            StopCoroutine(
                signalCoroutine
            );


            signalCoroutine =
                null;
        }


        TurnOffLight();
    }


    // ============================================================
    // References
    // ============================================================

    private void ResolveReferences()
    {
        if (periscopeReference == null)
        {
            PeriscopeController
                periscopeController =
                    FindFirstObjectByType<
                        PeriscopeController
                    >();


            if (periscopeController != null)
            {
                periscopeReference =
                    periscopeController
                        .transform;
            }
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


        if (
            visualRoot == null &&
            transform.childCount > 0
        )
        {
            visualRoot =
                transform
                    .GetChild(0)
                    .gameObject;
        }
    }


    // ============================================================
    // Placement
    // ============================================================

    private void PlaceEnemyBehindPeriscope()
    {
        if (periscopeReference == null)
        {
            Debug.LogWarning(
                "TutorialEnemyControllerV2: " +
                "Periscope Referenceがありません。"
            );


            return;
        }


        // ========================================================
        // 潜望鏡の真後ろ方向
        // ========================================================

        Vector3 behindDirection =
            -periscopeReference.forward;


        behindDirection.y =
            0.0f;


        if (
            behindDirection.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            behindDirection =
                Vector3.back;
        }


        behindDirection.Normalize();


        // ========================================================
        // スポーン座標
        // ========================================================

        Vector3 position =
            periscopeReference.position +
            behindDirection *
            distanceFromPeriscope;


        position.y =
            enemyWorldY;


        transform.position =
            position;


        // ========================================================
        // 潜望鏡方向を基準とした従来の向き
        // ========================================================

        Vector3 directionToPeriscope =
            periscopeReference.position -
            transform.position;


        directionToPeriscope.y =
            0.0f;


        if (
            directionToPeriscope.sqrMagnitude >
            Mathf.Epsilon
        )
        {
            Quaternion lookRotation =
                Quaternion.LookRotation(
                    directionToPeriscope.normalized
                );


            // ====================================================
            // 従来の回転
            // ====================================================

            Quaternion baseRotation =
                lookRotation *
                Quaternion.Euler(
                    0.0f,
                    modelRotationOffset,
                    0.0f
                );


            // ====================================================
            // 今回追加
            //
            // 従来の向きから、
            // さらにY軸方向へ90度回転させる。
            // ====================================================

            Quaternion additionalRotation =
                Quaternion.Euler(
                    0.0f,
                    additionalSpawnRotationY,
                    0.0f
                );


            transform.rotation =
                baseRotation *
                additionalRotation;
        }


        if (debugLog)
        {
            Debug.Log(
                "Tutorial enemy spawned. " +
                "Position = " +
                transform.position +
                ", Rotation Y = " +
                transform.eulerAngles.y
            );
        }
    }


    // ============================================================
    // Detection
    // ============================================================

    public void SetDetectionEnabled(
        bool enabled
    )
    {
        detectionEnabled =
            enabled &&
            !enemyFound;


        observationTime =
            0.0f;
    }


    private void UpdateDetection()
    {
        bool visible =
            IsInsidePeriscopeView();


        if (!visible)
        {
            observationTime =
                0.0f;


            return;
        }


        observationTime +=
            Time.deltaTime;


        if (
            observationTime <
            requiredObservationTime
        )
        {
            return;
        }


        CompleteDetection();
    }


    private bool IsInsidePeriscopeView()
    {
        if (
            !DataManager
                .GetIsPeriscopeAboveSurface()
        )
        {
            return false;
        }


        Vector3 periscopePosition =
            DataManager
                .GetPeriscopePosition();


        float periscopeYaw =
            DataManager
                .GetPeriscopeRotation();


        Vector3 periscopeForward =
            Quaternion.Euler(
                0.0f,
                periscopeYaw,
                0.0f
            )
            *
            Vector3.forward;


        Vector3 directionToEnemy =
            transform.position -
            periscopePosition;


        directionToEnemy.y =
            0.0f;


        periscopeForward.y =
            0.0f;


        if (
            directionToEnemy.magnitude >
            maximumDetectionDistance
        )
        {
            return false;
        }


        if (
            directionToEnemy.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return false;
        }


        float angle =
            Vector3.Angle(
                periscopeForward.normalized,
                directionToEnemy.normalized
            );


        return
            angle <=
            detectionFOV *
            0.5f;
    }


    private void CompleteDetection()
    {
        if (enemyFound)
        {
            return;
        }


        enemyFound =
            true;


        detectionEnabled =
            false;


        if (debugLog)
        {
            Debug.Log(
                "Tutorial: 雪風を発見しました。"
            );
        }


        EnemyFound?.Invoke();
    }


    // ============================================================
    // Signal Mission
    // ============================================================

    public bool BeginSignalMission()
    {
        EnsureSignalPattern();


        if (!enemyFound)
        {
            Debug.LogWarning(
                "TutorialEnemyControllerV2: " +
                "雪風発見前なので信号を開始できません。"
            );


            return false;
        }


        if (
            enemyShip == null ||
            communicationMissionManager ==
            null
        )
        {
            Debug.LogError(
                "TutorialEnemyControllerV2: " +
                "EnemyShipまたはCommunicationMissionManagerがありません。"
            );


            return false;
        }


        bool accepted =
            communicationMissionManager
                .TryBeginMission(
                    enemyShip,
                    tutorialSignalPattern
                );


        if (!accepted)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "Tutorial: " +
                    "CommunicationMissionManagerが" +
                    "信号開始を受け付けませんでした。"
                );
            }


            return false;
        }


        if (signalCoroutine != null)
        {
            StopCoroutine(
                signalCoroutine
            );
        }


        signalCoroutine =
            StartCoroutine(
                SignalRoutine()
            );


        return true;
    }


    private void EnsureSignalPattern()
    {
        if (
            tutorialSignalPattern != null &&
            tutorialSignalPattern.Length > 0
        )
        {
            return;
        }


        tutorialSignalPattern =
            new SignalSymbol[]
            {
                SignalSymbol.Short,
                SignalSymbol.Short,
                SignalSymbol.Long,
                SignalSymbol.Short
            };
    }


    // ============================================================
    // Light
    // ============================================================

    private void CreateSignalLight()
    {
        if (signalLight != null)
        {
            return;
        }


        GameObject lightObject =
            new GameObject(
                "TutorialSignalLight"
            );


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


        signalLight.intensity =
            signalIntensity;


        signalLight.range =
            signalRange;


        signalLight.enabled =
            false;
    }


    // ============================================================
    // Signal
    // ============================================================

    private IEnumerator SignalRoutine()
    {
        int actualRepeatCount =
            Mathf.Max(
                1,
                repeatCount
            );


        for (
            int repeatIndex = 0;
            repeatIndex < actualRepeatCount;
            repeatIndex++
        )
        {
            // ====================================================
            // 黄色：周期開始
            // ====================================================

            yield return
                PlayFlash(
                    startMarkerColor,
                    startMarkerDuration,
                    startMarkerBlankDuration
                );


            // ====================================================
            // 信号本体
            // ====================================================

            for (
                int symbolIndex = 0;
                symbolIndex <
                tutorialSignalPattern.Length;
                symbolIndex++
            )
            {
                SignalSymbol symbol =
                    tutorialSignalPattern[
                        symbolIndex
                    ];


                Color color =
                    symbol ==
                    SignalSymbol.Short
                        ? shortSignalColor
                        : longSignalColor;


                float duration =
                    symbol ==
                    SignalSymbol.Short
                        ? shortSignalDuration
                        : longSignalDuration;


                yield return
                    PlayFlash(
                        color,
                        duration,
                        symbolBlankDuration
                    );
            }


            if (
                repeatIndex <
                actualRepeatCount - 1
            )
            {
                yield return
                    new WaitForSecondsRealtime(
                        repeatInterval
                    );
            }
        }


        signalCoroutine =
            null;


        TurnOffLight();


        communicationMissionManager
            .NotifyEnemySignalFinished(
                enemyShip
            );


        if (debugLog)
        {
            Debug.Log(
                "Tutorial: 発光信号終了"
            );
        }


        SignalFinished?.Invoke();
    }


    private IEnumerator PlayFlash(
        Color color,
        float duration,
        float blankDuration
    )
    {
        if (signalLight == null)
        {
            yield break;
        }


        signalLight.color =
            color;


        signalLight.enabled =
            true;


        if (duration > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    duration
                );
        }


        signalLight.enabled =
            false;


        if (blankDuration > 0.0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    blankDuration
                );
        }
    }


    private void TurnOffLight()
    {
        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }
    }


    // ============================================================
    // Getter
    // ============================================================

    public bool GetIsEnemyFound()
    {
        return
            enemyFound;
    }


    public SignalSymbol[] GetTutorialSignalPattern()
    {
        return
            tutorialSignalPattern;
    }


    // ============================================================
    // Inspector
    // ============================================================

    private void OnValidate()
    {
        distanceFromPeriscope =
            Mathf.Max(
                1.0f,
                distanceFromPeriscope
            );


        detectionFOV =
            Mathf.Clamp(
                detectionFOV,
                1.0f,
                179.0f
            );


        maximumDetectionDistance =
            Mathf.Max(
                0.1f,
                maximumDetectionDistance
            );


        requiredObservationTime =
            Mathf.Max(
                0.0f,
                requiredObservationTime
            );


        repeatCount =
            Mathf.Max(
                1,
                repeatCount
            );


        startMarkerDuration =
            Mathf.Max(
                0.0f,
                startMarkerDuration
            );


        startMarkerBlankDuration =
            Mathf.Max(
                0.0f,
                startMarkerBlankDuration
            );


        shortSignalDuration =
            Mathf.Max(
                0.0f,
                shortSignalDuration
            );


        longSignalDuration =
            Mathf.Max(
                0.0f,
                longSignalDuration
            );


        symbolBlankDuration =
            Mathf.Max(
                0.0f,
                symbolBlankDuration
            );


        repeatInterval =
            Mathf.Max(
                0.0f,
                repeatInterval
            );


        signalIntensity =
            Mathf.Max(
                0.0f,
                signalIntensity
            );


        signalRange =
            Mathf.Max(
                0.0f,
                signalRange
            );
    }
}