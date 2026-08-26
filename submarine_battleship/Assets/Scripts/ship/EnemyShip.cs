using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(
    typeof(Rigidbody),
    typeof(SurfaceContact)
)]
[DisallowMultipleComponent]
public class EnemyShip : Ship
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultMovementSpeed =
        0.2f;

    private const float DefaultRadiusRandomFactor =
        0.05f;

    private const float DefaultModelRotationOffset =
        90.0f;


    private const float DefaultPeriscopeFOV =
        45.0f;

    private const float DefaultMaximumDetectionDistance =
        50.0f;

    private const float MinimumPeriscopeFOV =
        1.0f;

    private const float MaximumPeriscopeFOV =
        179.0f;


    // ============================================================
    // 観察時間
    // ============================================================

    private const float DefaultRequiredObservationTime =
        1.5f;


    // ============================================================
    // 信号パターン
    // ============================================================

    private const int DefaultMinimumSignalLength =
        4;

    private const int DefaultMaximumSignalLength =
        4;

    private const int MinimumSignalLength =
        1;


    // ============================================================
    // 信号時間
    // ============================================================

    private const float DefaultStartMarkerDuration =
        0.4f;

    private const float DefaultStartMarkerBlankDuration =
        0.3f;

    private const float DefaultShortSignalDuration =
        0.15f;

    private const float DefaultLongSignalDuration =
        0.65f;

    private const float DefaultSymbolBlankDuration =
        0.15f;


    private const int DefaultSignalRepeatCount =
        1;

    private const int MinimumSignalRepeatCount =
        1;

    private const float DefaultSignalRepeatInterval =
        1.5f;


    // ============================================================
    // 信号ライト
    // ============================================================

    private const float DefaultSignalIntensity =
        1500.0f;

    private const float DefaultSignalRange =
        150.0f;


    // ============================================================
    // その他
    // ============================================================

    private const float ShortSignalProbability =
        0.5f;

    private const float MinimumNonNegativeValue =
        0.0f;


    private static readonly float FullCircleRadians =
        Mathf.PI *
        2.0f;


    private static readonly Vector3
        DefaultSignalLightLocalPosition =
            new Vector3(
                0.0f,
                3.0f,
                0.0f
            );


    // ============================================================
    // デフォルト色
    // ============================================================

    private static readonly Color
        DefaultStartMarkerColor =
            Color.yellow;


    private static readonly Color
        DefaultShortSignalColor =
            Color.red;


    private static readonly Color
        DefaultLongSignalColor =
            new Color(
                1.0f,
                0.5f,
                0.0f,
                1.0f
            );


    // ============================================================
    // 移動
    // ============================================================

    [Header("Movement")]

    [SerializeField, Tooltip(
        "敵艦が円運動するときの角速度")]
    [Min(MinimumNonNegativeValue)]
    private float movementSpeed =
        DefaultMovementSpeed;


    [SerializeField, Tooltip(
        "敵艦ごとの回転半径に加えるランダム幅。" +
        "0.05なら基準半径の±5%")]
    [Range(0.0f, 1.0f)]
    private float radiusRandomFactor =
        DefaultRadiusRandomFactor;


    [SerializeField, Tooltip(
        "船モデルの進行方向に対するY回転補正")]
    private float modelRotationOffset =
        DefaultModelRotationOffset;


    // ============================================================
    // 潜望鏡による発見
    // ============================================================

    [Header("Periscope Detection")]

    [SerializeField, Tooltip(
        "敵艦を発見できる潜望鏡の視野角。" +
        "左右合計の角度")]
    [Range(
        MinimumPeriscopeFOV,
        MaximumPeriscopeFOV
    )]
    private float periscopeFOV =
        DefaultPeriscopeFOV;


    [SerializeField, Tooltip(
        "潜望鏡から敵艦を発見できる最大距離")]
    [Min(MinimumNonNegativeValue)]
    private float maximumDetectionDistance =
        DefaultMaximumDetectionDistance;


    [SerializeField, Tooltip(
        "発見されるまで敵艦モデルを非表示にする。" +
        "通常ゲームではOFF推奨")]
    private bool hideUntilDetected =
        false;


    [SerializeField, Tooltip(
        "非表示切替対象。" +
        "未設定なら最初の子GameObjectを使用する")]
    private GameObject shipVisual;


    // ============================================================
    // 信号開始までの観察
    // ============================================================

    [Header("Signal Preparation")]

    [SerializeField, Tooltip(
        "敵艦を潜望鏡に捉えてから" +
        "信号が始まるまでに必要な時間")]
    [Min(MinimumNonNegativeValue)]
    private float requiredObservationTimeBeforeSignal =
        DefaultRequiredObservationTime;


    [SerializeField, Tooltip(
        "ONの場合、信号開始まで敵艦を" +
        "潜望鏡の視界内に捉え続ける必要がある。" +
        "途中で視界から外れると観察時間をリセットする")]
    private bool requireContinuousObservationUntilSignal =
        true;


    // ============================================================
    // 信号パターン
    // ============================================================

    [Header("Signal Pattern")]

    [SerializeField, Tooltip(
        "信号を構成する最小記号数")]
    [Min(MinimumSignalLength)]
    private int minimumSignalLength =
        DefaultMinimumSignalLength;


    [SerializeField, Tooltip(
        "信号を構成する最大記号数")]
    [Min(MinimumSignalLength)]
    private int maximumSignalLength =
        DefaultMaximumSignalLength;


    [SerializeField, Tooltip(
        "同じ信号を何回繰り返すか。" +
        "各繰り返しの先頭では黄色の開始合図を点灯する")]
    [Min(MinimumSignalRepeatCount)]
    private int signalRepeatCount =
        DefaultSignalRepeatCount;


    // ============================================================
    // 開始合図
    // ============================================================

    [Header("Cycle Start Marker")]

    [SerializeField, Tooltip(
        "各信号周期の先頭で点灯する開始合図の色")]
    private Color startMarkerColor =
        DefaultStartMarkerColor;


    [SerializeField, Tooltip(
        "黄色の開始合図を点灯する時間")]
    [Min(MinimumNonNegativeValue)]
    private float startMarkerDuration =
        DefaultStartMarkerDuration;


    [SerializeField, Tooltip(
        "開始合図が消えてから最初の信号までの待ち時間")]
    [Min(MinimumNonNegativeValue)]
    private float startMarkerBlankDuration =
        DefaultStartMarkerBlankDuration;


    // ============================================================
    // 信号時間
    // ============================================================

    [Header("Signal Timing")]

    [SerializeField, Tooltip(
        "短信号「・」の点灯時間")]
    [Min(MinimumNonNegativeValue)]
    private float shortSignalDuration =
        DefaultShortSignalDuration;


    [SerializeField, Tooltip(
        "長信号「―」の点灯時間")]
    [Min(MinimumNonNegativeValue)]
    private float longSignalDuration =
        DefaultLongSignalDuration;


    [SerializeField, Tooltip(
        "各信号記号の間の消灯時間")]
    [Min(MinimumNonNegativeValue)]
    private float symbolBlankDuration =
        DefaultSymbolBlankDuration;


    [SerializeField, Tooltip(
        "1周期終了から次の黄色い開始合図までの待ち時間")]
    [Min(MinimumNonNegativeValue)]
    private float signalRepeatInterval =
        DefaultSignalRepeatInterval;


    // ============================================================
    // 信号色
    // ============================================================

    [Header("Signal Colors")]

    [FormerlySerializedAs("signalColor")]
    [SerializeField, Tooltip(
        "短信号「・」の色")]
    private Color shortSignalColor =
        DefaultShortSignalColor;


    [SerializeField, Tooltip(
        "長信号「―」の色")]
    private Color longSignalColor =
        DefaultLongSignalColor;


    // ============================================================
    // 信号ライト
    // ============================================================

    [Header("Signal Light")]

    [SerializeField, Tooltip(
        "信号ライトのローカル座標")]
    private Vector3 signalLightLocalPosition =
        new Vector3(
            DefaultSignalLightLocalPosition.x,
            DefaultSignalLightLocalPosition.y,
            DefaultSignalLightLocalPosition.z
        );


    [SerializeField, Tooltip(
        "信号ライトの明るさ")]
    [Min(MinimumNonNegativeValue)]
    private float signalIntensity =
        DefaultSignalIntensity;


    [SerializeField, Tooltip(
        "信号ライトの届く範囲")]
    [Min(MinimumNonNegativeValue)]
    private float signalRange =
        DefaultSignalRange;


    [SerializeField, Tooltip(
        "使用するLightの種類")]
    private LightType signalLightType =
        LightType.Point;


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField, Tooltip(
        "通信ミッションを管理するManager。" +
        "未設定なら自動検索する")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "観察・信号開始などをConsoleへ表示する")]
    private bool debugLog =
        false;


    // ============================================================
    // 内部状態
    // ============================================================

    private Rigidbody shipRigidbody;

    private SurfaceContact surfaceContact;


    private Vector3 centerPoint;

    private float movementRadius;

    private float currentMovementAngle;


    private Light signalLight;

    private Coroutine signalCoroutine;


    private readonly List<SignalSymbol>
        signalPattern =
            new List<SignalSymbol>();


    // ============================================================
    // 信号状態
    // ============================================================

    private bool isDetected =
        false;


    private bool signalStarted =
        false;


    private bool signalFinished =
        false;


    // ============================================================
    // 観察状態
    // ============================================================

    private bool observationStarted =
        false;


    private bool targetCurrentlyInView =
        false;


    private float currentObservationTime =
        MinimumNonNegativeValue;


    // ============================================================
    // Start
    // ============================================================

    protected override void Start()
    {
        base.Start();


        // =========================
        // Rigidbody
        // =========================

        shipRigidbody =
            GetComponent<Rigidbody>();


        if (shipRigidbody == null)
        {
            Debug.LogError(
                "EnemyShipにRigidbodyがありません。"
            );


            enabled =
                false;


            return;
        }


        // =========================
        // SurfaceContact
        // =========================

        surfaceContact =
            GetComponent<SurfaceContact>();


        if (surfaceContact == null)
        {
            surfaceContact =
                gameObject
                    .AddComponent<SurfaceContact>();
        }


        surfaceContact.SetContactType(
            SurfaceContactType.Enemy
        );


        surfaceContact.SetSonarDetectable(
            true
        );


        // =========================
        // MissionManager
        // =========================

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
            communicationMissionManager ==
            null
        )
        {
            Debug.LogWarning(
                "CommunicationMissionManagerが見つかりません。"
            );
        }


        // =========================
        // 見た目
        // =========================

        ResolveShipVisual();


        // =========================
        // 移動
        // =========================

        InitializeMovement();


        // =========================
        // 信号パターン
        // =========================

        GenerateSignalPattern();


        // =========================
        // ライト
        // =========================

        CreateSignalLight();


        // =========================
        // Rigidbody
        // =========================

        shipRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;
    }


    // ============================================================
    // Update
    // ============================================================

    protected override void Update()
    {
        base.Update();


        if (
            isDetected ||
            signalStarted ||
            signalFinished
        )
        {
            return;
        }


        UpdatePeriscopeObservation();
    }


    // ============================================================
    // FixedUpdate
    // ============================================================

    protected override void FixedUpdate()
    {
        if (shipRigidbody == null)
        {
            return;
        }


        UpdateCircularMovement();
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


        TurnOffSignalLight();


        if (
            communicationMissionManager !=
            null
        )
        {
            communicationMissionManager
                .NotifyEnemyDestroyed(
                    this
                );
        }
    }


    // ============================================================
    // 見た目
    // ============================================================

    private void ResolveShipVisual()
    {
        if (
            shipVisual == null &&
            transform.childCount > 0
        )
        {
            shipVisual =
                transform
                    .GetChild(0)
                    .gameObject;
        }


        if (
            shipVisual != null &&
            hideUntilDetected
        )
        {
            shipVisual.SetActive(
                false
            );
        }
    }


    // ============================================================
    // 移動初期化
    // ============================================================

    private void InitializeMovement()
    {
        float baseRadius =
            DataManager
                .GetEnemyShipRotateRadius();


        float randomRadiusOffset =
            baseRadius *
            radiusRandomFactor;


        movementRadius =
            baseRadius +
            Random.Range(
                -randomRadiusOffset,
                randomRadiusOffset
            );


        movementRadius =
            Mathf.Max(
                MinimumNonNegativeValue,
                movementRadius
            );


        currentMovementAngle =
            Random.Range(
                MinimumNonNegativeValue,
                FullCircleRadians
            );


        Vector3 radialOffset =
            CalculateMovementRadialOffset(
                currentMovementAngle
            );


        // 現在のスポーン位置を
        // 円周上の初期位置として扱う
        centerPoint =
            shipRigidbody.position -
            radialOffset;
    }


    // ============================================================
    // 円運動
    // ============================================================

    private void UpdateCircularMovement()
    {
        if (
            movementRadius <=
            Mathf.Epsilon
        )
        {
            return;
        }


        currentMovementAngle +=
            movementSpeed *
            Time.fixedDeltaTime;


        currentMovementAngle =
            Mathf.Repeat(
                currentMovementAngle,
                FullCircleRadians
            );


        Vector3 radialOffset =
            CalculateMovementRadialOffset(
                currentMovementAngle
            );


        Vector3 nextPosition =
            centerPoint +
            radialOffset;


        Vector3 moveDirection =
            nextPosition -
            shipRigidbody.position;


        moveDirection.y =
            MinimumNonNegativeValue;


        if (
            moveDirection.sqrMagnitude >
            Mathf.Epsilon
        )
        {
            Quaternion lookRotation =
                Quaternion.LookRotation(
                    moveDirection.normalized
                );


            Quaternion correctedRotation =
                lookRotation *
                Quaternion.Euler(
                    MinimumNonNegativeValue,
                    modelRotationOffset,
                    MinimumNonNegativeValue
                );


            shipRigidbody.MoveRotation(
                correctedRotation
            );
        }


        shipRigidbody.MovePosition(
            nextPosition
        );
    }


    // ============================================================
    // 円運動オフセット
    // ============================================================

    private Vector3 CalculateMovementRadialOffset(
        float angle
    )
    {
        return
            new Vector3(
                Mathf.Cos(angle) *
                movementRadius,

                MinimumNonNegativeValue,

                Mathf.Sin(angle) *
                movementRadius
            );
    }


    // ============================================================
    // 潜望鏡観察
    // ============================================================

    private void UpdatePeriscopeObservation()
    {
        targetCurrentlyInView =
            IsEnemyInsidePeriscopeView();


        // ========================================================
        // 最初に視界へ入った瞬間
        // ========================================================

        if (!observationStarted)
        {
            if (!targetCurrentlyInView)
            {
                return;
            }


            observationStarted =
                true;


            currentObservationTime =
                MinimumNonNegativeValue;


            if (debugLog)
            {
                Debug.Log(
                    gameObject.name +
                    " の観察を開始しました。"
                );
            }
        }


        // ========================================================
        // 継続観察
        // ========================================================

        if (
            requireContinuousObservationUntilSignal &&
            !targetCurrentlyInView
        )
        {
            ResetObservation();


            return;
        }


        // ========================================================
        // 待ち時間なし
        // ========================================================

        if (
            requiredObservationTimeBeforeSignal <=
            MinimumNonNegativeValue
        )
        {
            TryDetectEnemy();


            return;
        }


        // ========================================================
        // 観察時間
        // ========================================================

        currentObservationTime +=
            Time.deltaTime;


        if (
            currentObservationTime <
            requiredObservationTimeBeforeSignal
        )
        {
            return;
        }


        currentObservationTime =
            requiredObservationTimeBeforeSignal;


        TryDetectEnemy();
    }


    // ============================================================
    // 敵艦が潜望鏡の視界内か
    // ============================================================

    private bool IsEnemyInsidePeriscopeView()
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
                MinimumNonNegativeValue,
                periscopeYaw,
                MinimumNonNegativeValue
            )
            *
            Vector3.forward;


        Vector3 directionToEnemy =
            transform.position -
            periscopePosition;


        directionToEnemy.y =
            MinimumNonNegativeValue;


        periscopeForward.y =
            MinimumNonNegativeValue;


        // =========================
        // 距離
        // =========================

        float distance =
            directionToEnemy.magnitude;


        if (
            distance >
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


        if (
            periscopeForward.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return false;
        }


        // =========================
        // 視野角
        // =========================

        float angle =
            Vector3.Angle(
                periscopeForward.normalized,
                directionToEnemy.normalized
            );


        float halfFOV =
            periscopeFOV *
            0.5f;


        return
            angle <=
            halfFOV;
    }


    // ============================================================
    // 観察リセット
    // ============================================================

    private void ResetObservation()
    {
        if (
            debugLog &&
            observationStarted
        )
        {
            Debug.Log(
                gameObject.name +
                " を視界から外したため、" +
                "観察時間をリセットしました。"
            );
        }


        observationStarted =
            false;


        currentObservationTime =
            MinimumNonNegativeValue;
    }


    // ============================================================
    // 発見確定
    // ============================================================

    private void TryDetectEnemy()
    {
        if (
            isDetected ||
            signalStarted ||
            signalFinished
        )
        {
            return;
        }


        // ========================================================
        // MissionManager
        // ========================================================

        if (
            communicationMissionManager !=
            null
        )
        {
            bool accepted =
                communicationMissionManager
                    .TryBeginMission(
                        this,
                        signalPattern
                    );


            if (!accepted)
            {
                ResetObservation();


                return;
            }
        }


        isDetected =
            true;


        if (
            shipVisual != null &&
            hideUntilDetected
        )
        {
            shipVisual.SetActive(
                true
            );
        }


        if (debugLog)
        {
            Debug.Log(
                gameObject.name +
                " の観察完了。信号を開始します。"
            );
        }


        StartSignal();
    }


    // ============================================================
    // 信号生成
    // ============================================================

    private void GenerateSignalPattern()
    {
        signalPattern.Clear();


        int minimumLength =
            Mathf.Max(
                MinimumSignalLength,
                minimumSignalLength
            );


        int maximumLength =
            Mathf.Max(
                minimumLength,
                maximumSignalLength
            );


        int signalLength =
            Random.Range(
                minimumLength,
                maximumLength + 1
            );


        for (
            int symbolIndex = 0;
            symbolIndex < signalLength;
            symbolIndex++
        )
        {
            bool shortSignal =
                Random.value <
                ShortSignalProbability;


            signalPattern.Add(
                shortSignal
                    ? SignalSymbol.Short
                    : SignalSymbol.Long
            );
        }
    }


    // ============================================================
    // 信号ライト作成
    // ============================================================

    private void CreateSignalLight()
    {
        GameObject lightObject =
            new GameObject(
                "AutoSignalLight"
            );


        lightObject.transform.SetParent(
            transform,
            false
        );


        lightObject.transform.localPosition =
            signalLightLocalPosition;


        signalLight =
            lightObject
                .AddComponent<Light>();


        signalLight.type =
            signalLightType;


        // 初期色。
        // 実際の点灯時には毎回変更する。
        signalLight.color =
            startMarkerColor;


        signalLight.intensity =
            signalIntensity;


        signalLight.range =
            signalRange;


        signalLight.enabled =
            false;
    }


    // ============================================================
    // 信号開始
    // ============================================================

    private void StartSignal()
    {
        if (
            signalStarted ||
            signalFinished
        )
        {
            return;
        }


        signalStarted =
            true;


        signalCoroutine =
            StartCoroutine(
                FlashSignalRoutine()
            );
    }


    // ============================================================
    // 信号全体
    // ============================================================

    private IEnumerator FlashSignalRoutine()
    {
        int repeatCount =
            Mathf.Max(
                MinimumSignalRepeatCount,
                signalRepeatCount
            );


        for (
            int repeatIndex = 0;
            repeatIndex < repeatCount;
            repeatIndex++
        )
        {
            // ====================================================
            // 各周期の開始合図
            // ====================================================
            //
            // 黄色
            // ↓
            // 消灯
            // ↓
            // 実際の信号
            // ====================================================

            yield return
                PlayColoredFlash(
                    startMarkerColor,
                    startMarkerDuration,
                    startMarkerBlankDuration
                );


            // ====================================================
            // 信号本体
            // ====================================================

            for (
                int symbolIndex = 0;
                symbolIndex < signalPattern.Count;
                symbolIndex++
            )
            {
                SignalSymbol symbol =
                    signalPattern[
                        symbolIndex
                    ];


                Color symbolColor =
                    GetSignalColor(
                        symbol
                    );


                float lightDuration =
                    GetSignalDuration(
                        symbol
                    );


                yield return
                    PlayColoredFlash(
                        symbolColor,
                        lightDuration,
                        symbolBlankDuration
                    );
            }


            // ====================================================
            // 次の周期まで待機
            // ====================================================

            if (
                repeatIndex <
                repeatCount - 1
            )
            {
                yield return
                    new WaitForSeconds(
                        signalRepeatInterval
                    );
            }
        }


        signalCoroutine =
            null;


        FinishSignal();
    }


    // ============================================================
    // 色付き点灯
    // ============================================================

    private IEnumerator PlayColoredFlash(
        Color lightColor,
        float lightDuration,
        float blankDuration
    )
    {
        if (signalLight == null)
        {
            yield break;
        }


        // =========================
        // 色を設定
        // =========================

        signalLight.color =
            lightColor;


        // =========================
        // 点灯
        // =========================

        signalLight.enabled =
            true;


        if (
            lightDuration >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    lightDuration
                );
        }


        // =========================
        // 消灯
        // =========================

        signalLight.enabled =
            false;


        if (
            blankDuration >
            MinimumNonNegativeValue
        )
        {
            yield return
                new WaitForSeconds(
                    blankDuration
                );
        }
    }


    // ============================================================
    // 記号ごとの色
    // ============================================================

    private Color GetSignalColor(
        SignalSymbol symbol
    )
    {
        switch (symbol)
        {
            case SignalSymbol.Short:

                return
                    shortSignalColor;


            case SignalSymbol.Long:

                return
                    longSignalColor;


            default:

                return
                    shortSignalColor;
        }
    }


    // ============================================================
    // 記号ごとの点灯時間
    // ============================================================

    private float GetSignalDuration(
        SignalSymbol symbol
    )
    {
        switch (symbol)
        {
            case SignalSymbol.Short:

                return
                    shortSignalDuration;


            case SignalSymbol.Long:

                return
                    longSignalDuration;


            default:

                return
                    shortSignalDuration;
        }
    }


    // ============================================================
    // ライト消灯
    // ============================================================

    private void TurnOffSignalLight()
    {
        if (signalLight == null)
        {
            return;
        }


        signalLight.enabled =
            false;
    }


    // ============================================================
    // 信号終了
    // ============================================================

    private void FinishSignal()
    {
        if (signalFinished)
        {
            return;
        }


        signalFinished =
            true;


        TurnOffSignalLight();


        if (
            communicationMissionManager !=
            null
        )
        {
            communicationMissionManager
                .NotifyEnemySignalFinished(
                    this
                );
        }
    }


    // ============================================================
    // 状態取得
    // ============================================================

    public bool GetIsDetected()
    {
        return
            isDetected;
    }


    public bool GetIsSignalStarted()
    {
        return
            signalStarted;
    }


    public bool GetIsSignalFinished()
    {
        return
            signalFinished;
    }


    public IReadOnlyList<SignalSymbol>
        GetSignalPattern()
    {
        return
            signalPattern;
    }


    // ============================================================
    // 観察状態
    // ============================================================

    public bool GetIsTargetCurrentlyInView()
    {
        return
            targetCurrentlyInView;
    }


    public bool GetIsObservationStarted()
    {
        return
            observationStarted;
    }


    public float GetCurrentObservationTime()
    {
        return
            currentObservationTime;
    }


    public float GetRequiredObservationTime()
    {
        return
            requiredObservationTimeBeforeSignal;
    }


    public float GetObservationProgressNormalized()
    {
        if (
            requiredObservationTimeBeforeSignal <=
            Mathf.Epsilon
        )
        {
            return
                isDetected
                    ? 1.0f
                    : MinimumNonNegativeValue;
        }


        return
            Mathf.Clamp01(
                currentObservationTime /
                requiredObservationTimeBeforeSignal
            );
    }


    // ============================================================
    // 移動情報
    // ============================================================

    public Vector3 GetMovementCenter()
    {
        return
            centerPoint;
    }


    public float GetMovementRadius()
    {
        return
            movementRadius;
    }


    // ============================================================
    // Inspector検証
    // ============================================================

    private void OnValidate()
    {
        movementSpeed =
            Mathf.Max(
                MinimumNonNegativeValue,
                movementSpeed
            );


        radiusRandomFactor =
            Mathf.Clamp01(
                radiusRandomFactor
            );


        periscopeFOV =
            Mathf.Clamp(
                periscopeFOV,
                MinimumPeriscopeFOV,
                MaximumPeriscopeFOV
            );


        maximumDetectionDistance =
            Mathf.Max(
                MinimumNonNegativeValue,
                maximumDetectionDistance
            );


        requiredObservationTimeBeforeSignal =
            Mathf.Max(
                MinimumNonNegativeValue,
                requiredObservationTimeBeforeSignal
            );


        minimumSignalLength =
            Mathf.Max(
                MinimumSignalLength,
                minimumSignalLength
            );


        maximumSignalLength =
            Mathf.Max(
                minimumSignalLength,
                maximumSignalLength
            );


        signalRepeatCount =
            Mathf.Max(
                MinimumSignalRepeatCount,
                signalRepeatCount
            );


        startMarkerDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                startMarkerDuration
            );


        startMarkerBlankDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                startMarkerBlankDuration
            );


        shortSignalDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                shortSignalDuration
            );


        longSignalDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                longSignalDuration
            );


        symbolBlankDuration =
            Mathf.Max(
                MinimumNonNegativeValue,
                symbolBlankDuration
            );


        signalRepeatInterval =
            Mathf.Max(
                MinimumNonNegativeValue,
                signalRepeatInterval
            );


        signalIntensity =
            Mathf.Max(
                MinimumNonNegativeValue,
                signalIntensity
            );


        signalRange =
            Mathf.Max(
                MinimumNonNegativeValue,
                signalRange
            );
    }
}