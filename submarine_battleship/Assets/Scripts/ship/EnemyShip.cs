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

    private const float MinimumPeriscopeFOV = 1.0f;
    private const float MaximumPeriscopeFOV = 179.0f;

    private const int DefaultMinimumSignalLength = 4;
    private const int DefaultMaximumSignalLength = 4;

    private const float DefaultShortSignalDuration = 0.15f;
    private const float DefaultLongSignalDuration = 0.65f;
    private const float DefaultSymbolBlankDuration = 0.15f;

    private const int DefaultSignalRepeatCount = 1;
    private const float DefaultSignalRepeatInterval = 1.5f;

    private const float DefaultSignalIntensity = 1500.0f;
    private const float DefaultSignalRange = 150.0f;

    private const float ShortSignalProbability = 0.5f;

    private const float MinimumNonNegativeValue = 0.0f;

    private const int MinimumSignalLength = 1;
    private const int MinimumSignalRepeatCount = 1;

    private const float FullCircleRadians =
        Mathf.PI * 2.0f;


    private static readonly Vector3 DefaultSignalLightLocalPosition =
        new Vector3(
            0.0f,
            3.0f,
            0.0f
        );


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
        "敵艦を発見できる潜望鏡の視野角。" +
        "設定値は左右合計の角度")]
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
        "発見されるまでは敵艦モデルを非表示にする。" +
        "通常のゲームではOFF推奨")]
    private bool hideUntilDetected =
        false;


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
            DefaultSignalLightLocalPosition.x,
            DefaultSignalLightLocalPosition.y,
            DefaultSignalLightLocalPosition.z
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
            new List<SignalSymbol>();


    private bool isDetected =
        false;

    private bool signalStarted =
        false;

    private bool signalFinished =
        false;


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
        // 信号ライト
        // =========================

        CreateSignalLight();


        // =========================
        // Rigidbody設定
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
        // =========================
        // 基準となる円運動半径
        // =========================

        float baseRadius =
            DataManager
                .GetEnemyShipRotateRadius();


        // =========================
        // 艦ごとの半径ランダム差
        // =========================

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


        // =========================
        // 円周上の開始角度
        // =========================
        //
        // 各敵艦が毎回同じ位置関係の
        // 円運動にならないようランダム化する。
        // =========================

        currentMovementAngle =
            Random.Range(
                MinimumNonNegativeValue,
                FullCircleRadians
            );


        // =========================
        // 円の中心を逆算
        // =========================
        //
        // GameManagerがInstantiateした現在位置を
        // 「円周上の初期位置」として扱う。
        //
        // 以前のコードのように、
        // Start時にmovementRadius分だけ
        // 敵艦が突然移動することを防止する。
        // =========================

        Vector3 radialOffset =
            new Vector3(
                Mathf.Cos(
                    currentMovementAngle
                ) *
                movementRadius,

                MinimumNonNegativeValue,

                Mathf.Sin(
                    currentMovementAngle
                ) *
                movementRadius
            );


        centerPoint =
            shipRigidbody.position -
            radialOffset;
    }


    // ============================================================
    // 円運動
    // ============================================================

    private void UpdateCircularMovement()
    {
        // =========================
        // 半径が0なら移動しない
        // =========================

        if (
            movementRadius <=
            MinimumNonNegativeValue
        )
        {
            return;
        }


        // =========================
        // 角度更新
        // =========================

        currentMovementAngle +=
            movementSpeed *
            Time.fixedDeltaTime;


        // 値が無制限に増え続けないよう
        // 0～2πの範囲へ戻す
        currentMovementAngle =
            Mathf.Repeat(
                currentMovementAngle,
                FullCircleRadians
            );


        // =========================
        // 円周上の位置
        // =========================

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
                MinimumNonNegativeValue,
                z
            );


        // =========================
        // 移動方向
        // =========================

        Vector3 moveDirection =
            nextPosition -
            shipRigidbody.position;


        // 船なので水平方向だけを見る
        moveDirection.y =
            MinimumNonNegativeValue;


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
                    MinimumNonNegativeValue,
                    modelRotationOffset,
                    MinimumNonNegativeValue
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
        // =========================
        // 潜望鏡が海面下なら発見不可
        // =========================

        if (
            !DataManager
                .GetIsPeriscopeAboveSurface()
        )
        {
            return;
        }


        // =========================
        // 潜望鏡位置
        // =========================

        Vector3 periscopePosition =
            DataManager
                .GetPeriscopePosition();


        // =========================
        // 潜望鏡Yaw
        // =========================

        float periscopeYaw =
            DataManager
                .GetPeriscopeRotation();


        // =========================
        // 潜望鏡正面方向
        // =========================

        Vector3 periscopeForward =
            Quaternion.Euler(
                MinimumNonNegativeValue,
                periscopeYaw,
                MinimumNonNegativeValue
            ) *
            Vector3.forward;


        // =========================
        // 敵艦方向
        // =========================

        Vector3 directionToEnemy =
            transform.position -
            periscopePosition;


        // 水平方向だけで判定
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
            return;
        }


        // ほぼ同じ位置なら
        // 方向ベクトルを作れないため終了
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


        // periscopeFOVは全体角なので
        // 左右それぞれ半分
        float halfFOV =
            periscopeFOV *
            0.5f;


        if (
            angle >
            halfFOV
        )
        {
            return;
        }


        // =========================
        // 発見処理
        // =========================

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


        // =========================
        // MissionManager確認
        // =========================
        //
        // 他の敵艦の通信処理中などで
        // MissionManagerが受け付けられない場合は
        // 発見確定しない。
        // =========================

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


        // =========================
        // 発見確定
        // =========================

        isDetected =
            true;


        // =========================
        // 非表示だった艦を表示
        // =========================

        if (
            shipVisual != null &&
            hideUntilDetected
        )
        {
            shipVisual.SetActive(
                true
            );
        }


        // =========================
        // 信号開始
        // =========================

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
                ShortSignalProbability;


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
        // =========================
        // Light用GameObject
        // =========================

        GameObject lightObject =
            new GameObject(
                "AutoSignalLight"
            );


        // =========================
        // 敵艦の子にする
        // =========================

        lightObject.transform.SetParent(
            transform,
            false
        );


        // =========================
        // 位置
        // =========================

        lightObject.transform.localPosition =
            signalLightLocalPosition;


        // =========================
        // Light
        // =========================

        signalLight =
            lightObject
                .AddComponent<Light>();


        signalLight.type =
            signalLightType;


        signalLight.color =
            signalColor;


        signalLight.intensity =
            signalIntensity;


        signalLight.range =
            signalRange;


        // 最初は消灯
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


        // =========================
        // 信号全体の繰り返し
        // =========================

        for (
            int repeatIndex = 0;
            repeatIndex < repeatCount;
            repeatIndex++
        )
        {
            // =========================
            // 各記号
            // =========================

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


            // =========================
            // 次の繰り返しまで待機
            // =========================

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


        // =========================
        // 点灯
        // =========================

        signalLight.enabled =
            true;


        yield return
            new WaitForSeconds(
                lightDuration
            );


        // =========================
        // 消灯
        // =========================

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


        // =========================
        // ライト消灯
        // =========================

        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }


        // =========================
        // MissionManager通知
        // =========================

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
    // 移動情報取得
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
    // Inspector値検証
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