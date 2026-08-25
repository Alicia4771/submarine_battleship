using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyShip : Ship
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultMovementSpeed = 0.2f;
    private const float DefaultRadiusRandomFactor = 0.05f;

    private const float DefaultModelRotationOffset = 90.0f;

    private const float DefaultPeriscopeFOV = 45.0f;
    private const float DefaultMaximumDetectionDistance = 50.0f;

    private const int DefaultMinimumSignalLength = 4;
    private const int DefaultMaximumSignalLength = 4;

    private const float DefaultShortSignalDuration = 0.15f;
    private const float DefaultLongSignalDuration = 0.65f;
    private const float DefaultSymbolBlankDuration = 0.15f;

    private const int DefaultSignalRepeatCount = 1;
    private const float DefaultSignalRepeatInterval = 1.5f;

    private const float DefaultSignalIntensity = 1500.0f;
    private const float DefaultSignalRange = 150.0f;

    private const float MinimumNonNegativeValue = 0.0f;
    private const int MinimumSignalLength = 1;
    private const int MinimumSignalRepeatCount = 1;


    // ============================================================
    // 移動設定
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
        "船モデルの向きを補正する角度")]
    private float modelRotationOffset =
        DefaultModelRotationOffset;


    // ============================================================
    // 潜望鏡による発見設定
    // ============================================================

    [Header("Periscope Detection")]

    [SerializeField, Tooltip(
        "敵艦を発見できる潜望鏡の視野角")]
    [Range(1.0f, 179.0f)]
    private float periscopeFOV =
        DefaultPeriscopeFOV;


    [SerializeField, Tooltip(
        "潜望鏡から敵艦を発見できる最大距離")]
    [Min(MinimumNonNegativeValue)]
    private float maximumDetectionDistance =
        DefaultMaximumDetectionDistance;


    [SerializeField, Tooltip(
        "発見されるまでは敵艦モデルを非表示にする。" +
        "通常のゲームではOFF推奨")]
    private bool hideUntilDetected = false;


    [SerializeField, Tooltip(
        "非表示切替の対象。" +
        "未設定の場合は最初の子GameObjectを使用する")]
    private GameObject shipVisual;


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
        "同じ信号を何回繰り返すか")]
    [Min(MinimumSignalRepeatCount)]
    private int signalRepeatCount =
        DefaultSignalRepeatCount;


    // ============================================================
    // 信号時間
    // ============================================================

    [Header("Signal Timing")]

    [SerializeField, Tooltip(
        "短信号の点灯時間")]
    [Min(MinimumNonNegativeValue)]
    private float shortSignalDuration =
        DefaultShortSignalDuration;


    [SerializeField, Tooltip(
        "長信号の点灯時間")]
    [Min(MinimumNonNegativeValue)]
    private float longSignalDuration =
        DefaultLongSignalDuration;


    [SerializeField, Tooltip(
        "各記号の間の消灯時間")]
    [Min(MinimumNonNegativeValue)]
    private float symbolBlankDuration =
        DefaultSymbolBlankDuration;


    [SerializeField, Tooltip(
        "信号を複数回繰り返す場合の待ち時間")]
    [Min(MinimumNonNegativeValue)]
    private float signalRepeatInterval =
        DefaultSignalRepeatInterval;


    // ============================================================
    // 信号ライト
    // ============================================================

    [Header("Signal Light")]

    [SerializeField, Tooltip(
        "信号ライトのローカル座標")]
    private Vector3 signalLightLocalPosition =
        new Vector3(
            0.0f,
            3.0f,
            0.0f
        );


    [SerializeField, Tooltip(
        "信号ライトの色")]
    private Color signalColor =
        Color.red;


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
        "使用するLightの種類。" +
        "現在はPointを推奨")]
    private LightType signalLightType =
        LightType.Point;


    // ============================================================
    // 連携
    // ============================================================

    [Header("Mission")]

    [SerializeField, Tooltip(
        "通信ミッション全体を管理するManager。" +
        "未設定の場合は自動検索する")]
    private CommunicationMissionManager
        communicationMissionManager;


    // ============================================================
    // 内部状態
    // ============================================================

    private Rigidbody shipRigidbody;

    private Vector3 centerPoint;

    private float movementRadius;
    private float currentMovementAngle;


    private Light signalLight;

    private Coroutine signalCoroutine;


    private readonly List<SignalSymbol>
        signalPattern =
            new();


    private bool isDetected = false;
    private bool signalStarted = false;
    private bool signalFinished = false;


    // ============================================================
    // Start
    // ============================================================

    protected override void Start()
    {
        base.Start();


        shipRigidbody =
            GetComponent<Rigidbody>();


        if (shipRigidbody == null)
        {
            Debug.LogError(
                "EnemyShipにRigidbodyがありません。"
            );

            enabled = false;

            return;
        }


        // =========================
        // MissionManager取得
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


        // =========================
        // 見た目取得
        // =========================

        ResolveShipVisual();


        // =========================
        // 移動初期化
        // =========================

        InitializeMovement();


        // =========================
        // 信号生成
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


        // まだ通信対象として発見されていない場合だけ
        // 潜望鏡による発見判定を行う
        if (
            !isDetected &&
            !signalStarted &&
            !signalFinished
        )
        {
            CheckPeriscopeDetection();
        }
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


        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }


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
                transform.GetChild(
                    0
                ).gameObject;
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
        centerPoint =
            transform.position;


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
            0.0f;


        Vector3 initialPosition =
            centerPoint +
            new Vector3(
                movementRadius,
                0.0f,
                0.0f
            );


        shipRigidbody.position =
            initialPosition;
    }


    // ============================================================
    // 円運動
    // ============================================================

    private void UpdateCircularMovement()
    {
        currentMovementAngle +=
            movementSpeed *
            Time.fixedDeltaTime;


        float x =
            Mathf.Cos(
                currentMovementAngle
            ) *
            movementRadius;


        float z =
            Mathf.Sin(
                currentMovementAngle
            ) *
            movementRadius;


        Vector3 nextPosition =
            centerPoint +
            new Vector3(
                x,
                0.0f,
                z
            );


        Vector3 moveDirection =
            nextPosition -
            shipRigidbody.position;


        moveDirection.y =
            0.0f;


        // =========================
        // 向き
        // =========================

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
                    0.0f,
                    modelRotationOffset,
                    0.0f
                );


            shipRigidbody.MoveRotation(
                correctedRotation
            );
        }


        // =========================
        // 位置
        // =========================

        shipRigidbody.MovePosition(
            nextPosition
        );
    }


    // ============================================================
    // 潜望鏡による発見
    // ============================================================

    private void CheckPeriscopeDetection()
    {
        // 潜望鏡が海面下なら発見できない
        if (
            !DataManager
                .GetIsPeriscopeAboveSurface()
        )
        {
            return;
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
            ) *
            Vector3.forward;


        Vector3 directionToEnemy =
            transform.position -
            periscopePosition;


        // 水平方向だけで判定
        directionToEnemy.y =
            0.0f;

        periscopeForward.y =
            0.0f;


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
            return;
        }


        if (
            directionToEnemy.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return;
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


        if (angle > halfFOV)
        {
            return;
        }


        TryDetectEnemy();
    }


    // ============================================================
    // 発見
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


        // 他の敵艦の通信処理中などで
        // MissionManagerが受け付けられない場合は
        // 発見確定しない
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
            int i = 0;
            i < signalLength;
            i++
        )
        {
            bool shortSignal =
                Random.value <
                0.5f;


            signalPattern.Add(
                shortSignal
                    ? SignalSymbol.Short
                    : SignalSymbol.Long
            );
        }
    }


    // ============================================================
    // ライト生成
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
            lightObject.AddComponent<Light>();


        signalLight.type =
            signalLightType;


        signalLight.color =
            signalColor;


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
    // 信号コルーチン
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


                float lightDuration =
                    GetSignalDuration(
                        symbol
                    );


                yield return
                    PlayFlash(
                        lightDuration,
                        symbolBlankDuration
                    );
            }


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
    // 点灯
    // ============================================================

    private IEnumerator PlayFlash(
        float lightDuration,
        float blankDuration
    )
    {
        if (signalLight == null)
        {
            yield break;
        }


        signalLight.enabled =
            true;


        yield return
            new WaitForSeconds(
                lightDuration
            );


        signalLight.enabled =
            false;


        yield return
            new WaitForSeconds(
                blankDuration
            );
    }


    // ============================================================
    // 記号時間
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


        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }


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
    // Inspector値検証
    // ============================================================

    private void OnValidate()
    {
        movementSpeed =
            Mathf.Max(
                MinimumNonNegativeValue,
                movementSpeed
            );


        maximumDetectionDistance =
            Mathf.Max(
                MinimumNonNegativeValue,
                maximumDetectionDistance
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