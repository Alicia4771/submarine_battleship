using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(
    typeof(Rigidbody),
    typeof(SurfaceContact)
)]
[DisallowMultipleComponent]
public class ColorMemoryEnemyShip : Ship
{
    // ============================================================
    // Constants
    // ============================================================

    private const float MinimumValue =
        0.0f;


    private const float DefaultMinimumHorizontalDistanceFromSubmarine =
        15.0f;


    private const float DefaultAvoidanceClearance =
        1.0f;


    private const float DefaultAvoidanceStartDistance =
        25.0f;


    private const float DefaultOrbitCenterShiftSpeed =
        5.0f;


    private const float FullCircleRadians =
        Mathf.PI * 2.0f;


    // ============================================================
    // Movement
    // ============================================================

    [Header("Movement")]

    [SerializeField]
    private float movementSpeed =
        0.2f;


    [SerializeField, Range(0.0f, 1.0f)]
    private float radiusRandomFactor =
        0.05f;


    [SerializeField]
    private float modelRotationOffset =
        90.0f;


    // ============================================================
    // 潜水艦との接近回避
    // ============================================================

    [Header("Submarine Avoidance")]

    [SerializeField, Tooltip(
        "ONの場合、敵艦が潜水艦へ近づきすぎないようにする")]
    private bool enableSubmarineAvoidance =
        true;


    [SerializeField, Min(MinimumValue), Tooltip(
        "敵艦と潜水艦のXZ平面上で確保する最低距離")]
    private float minimumHorizontalDistanceFromSubmarine =
        DefaultMinimumHorizontalDistanceFromSubmarine;


    [SerializeField, Min(MinimumValue), Tooltip(
        "現在の周回軌道と潜水艦のXZ平面上の最短距離が" +
        "この値未満になると、新しい安全な周回中心へ滑らかに移行を開始する。" +
        "Minimum Horizontal Distance From Submarine以上を推奨")]
    private float avoidanceStartDistance =
        DefaultAvoidanceStartDistance;


    [SerializeField, Min(MinimumValue), Tooltip(
        "新しい安全な周回中心へ移動させる速度。" +
        "小さいほどゆっくり進路が曲がり、大きいほど早く安全側へ軌道を移す")]
    private float orbitCenterShiftSpeed =
        DefaultOrbitCenterShiftSpeed;


    [SerializeField, Min(MinimumValue), Tooltip(
        "回避目標に追加する余裕距離。" +
        "安全軌道との境界付近で何度も補正されるのを抑える")]
    private float avoidanceClearance =
        DefaultAvoidanceClearance;


    // ============================================================
    // Detection
    // ============================================================

    [Header("Periscope Detection")]

    [SerializeField, Range(1.0f, 179.0f)]
    private float periscopeFOV =
        45.0f;


    [SerializeField, Min(0.0f)]
    private float maximumDetectionDistance =
        50.0f;


    [SerializeField, Min(0.0f)]
    private float requiredObservationTime =
        1.5f;


    [SerializeField]
    private bool requireContinuousObservation =
        true;


    [SerializeField]
    private bool hideUntilDetected =
        false;


    [SerializeField]
    private GameObject shipVisual;


    // ============================================================
    // Sequence
    // ============================================================

    [Header("Color Sequence")]

    [SerializeField, Min(1)]
    private int minimumSequenceLength =
        4;


    [SerializeField, Min(1)]
    private int maximumSequenceLength =
        4;


    [SerializeField, Min(1)]
    private int sequenceRepeatCount =
        1;


    // ============================================================
    // Timing
    // ============================================================

    [Header("Timing")]

    [SerializeField]
    private Color startMarkerColor =
        Color.white;


    [SerializeField, Min(0.0f)]
    private float startMarkerDuration =
        0.4f;


    [SerializeField, Min(0.0f)]
    private float startMarkerBlankDuration =
        0.3f;


    [SerializeField, Min(0.0f)]
    private float colorDuration =
        0.6f;


    [SerializeField, Min(0.0f)]
    private float colorBlankDuration =
        0.25f;


    [SerializeField, Min(0.0f)]
    private float repeatInterval =
        1.5f;


    // ============================================================
    // Colors
    // ============================================================

    [Header("Colors")]

    [SerializeField]
    private Color redColor =
        Color.red;


    [SerializeField]
    private Color blueColor =
        Color.blue;


    [SerializeField]
    private Color yellowColor =
        Color.yellow;


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


    [SerializeField]
    private LightType signalLightType =
        LightType.Point;


    // ============================================================
    // Mission
    // ============================================================

    [Header("Mission")]

    [SerializeField]
    private ColorMemoryMissionManager
        colorMemoryMissionManager;


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


    private Vector3 centerPoint;

    private float movementRadius;

    private float movementAngle;


    private Light signalLight;


    private Coroutine signalCoroutine;


    private readonly List<ColorSignalSymbol>
        sequence =
            new List<ColorSignalSymbol>();


    private bool detected =
        false;


    private bool signalStarted =
        false;


    private bool signalFinished =
        false;


    private float observationTime =
        0.0f;


    // ============================================================
    // Start
    // ============================================================

    protected override void Start()
    {
        base.Start();


        shipRigidbody =
            GetComponent<Rigidbody>();


        surfaceContact =
            GetComponent<SurfaceContact>();


        surfaceContact.SetContactType(
            SurfaceContactType.Enemy
        );


        surfaceContact.SetSonarDetectable(
            true
        );


        if (
            colorMemoryMissionManager ==
            null
        )
        {
            colorMemoryMissionManager =
                FindFirstObjectByType<
                    ColorMemoryMissionManager
                >();
        }


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


        InitializeMovement();

        GenerateSequence();

        CreateSignalLight();


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
            detected ||
            signalStarted ||
            signalFinished
        )
        {
            return;
        }


        UpdateObservation();
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


        UpdateMovement();
    }


    // ============================================================
    // Destroy
    // ============================================================

    private void OnDestroy()
    {
        if (
            signalCoroutine !=
            null
        )
        {
            StopCoroutine(
                signalCoroutine
            );
        }


        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }


        if (
            colorMemoryMissionManager !=
            null
        )
        {
            colorMemoryMissionManager
                .NotifyEnemyDestroyed(
                    this
                );
        }
    }


    // ============================================================
    // Movement
    // ============================================================

    private void InitializeMovement()
    {
        float baseRadius =
            DataManager
                .GetEnemyShipRotateRadius();


        float offset =
            baseRadius *
            radiusRandomFactor;


        movementRadius =
            baseRadius +
            Random.Range(
                -offset,
                offset
            );


        movementAngle =
            Random.Range(
                0.0f,
                FullCircleRadians
            );


        Vector3 radial =
            GetRadialOffset(
                movementAngle
            );


        centerPoint =
            shipRigidbody.position -
            radial;
    }


    private void UpdateMovement()
    {
        if (
            movementRadius <=
            Mathf.Epsilon
        )
        {
            return;
        }


        // ========================================================
        // 潜水艦へ近づく可能性がある軌道なら、
        // 回転方向を反転させるのではなく、
        // 周回中心そのものを安全側へゆっくり移動させる。
        //
        // movementAngleは同じ向きに進め続けるため、
        // 急な切り返しは発生しない。
        // ========================================================

        if (enableSubmarineAvoidance)
        {
            UpdateOrbitCenterForSubmarineAvoidance();
        }


        float angularStep =
            movementSpeed *
            Time.fixedDeltaTime;


        float nextAngle =
            Mathf.Repeat(
                movementAngle +
                angularStep,

                FullCircleRadians
            );


        Vector3 nextPosition =
            centerPoint +
            GetRadialOffset(
                nextAngle
            );


        // ========================================================
        // 通常は事前の中心移動で回避する。
        // 潜水艦自身の移動などで最低距離内へ入る場合だけ、
        // 最後の安全処理を行う。
        //
        // この処理でも回転方向は反転しない。
        // ========================================================

        if (enableSubmarineAvoidance)
        {
            ApplyEmergencyMinimumDistance(
                ref nextPosition
            );
        }


        movementAngle =
            nextAngle;


        Vector3 direction =
            nextPosition -
            shipRigidbody.position;


        direction.y =
            MinimumValue;


        if (
            direction.sqrMagnitude >
            Mathf.Epsilon
        )
        {
            Quaternion rotation =
                Quaternion.LookRotation(
                    direction.normalized
                )
                *
                Quaternion.Euler(
                    MinimumValue,
                    modelRotationOffset,
                    MinimumValue
                );


            shipRigidbody.MoveRotation(
                rotation
            );
        }


        shipRigidbody.MovePosition(
            nextPosition
        );
    }


    // ============================================================
    // 周回中心を滑らかに移動して回避
    // ============================================================

    private void UpdateOrbitCenterForSubmarineAvoidance()
    {
        if (
            minimumHorizontalDistanceFromSubmarine <=
                MinimumValue
            ||
            orbitCenterShiftSpeed <=
                MinimumValue
        )
        {
            return;
        }


        Vector3 submarinePosition =
            DataManager
                .GetSubmarinePosition();


        float centerDistance =
            CalculateHorizontalDistance(
                centerPoint,
                submarinePosition
            );


        // 点から円周までの最短距離は
        // |中心までの距離 - 円の半径|
        float closestDistanceToCurrentOrbit =
            Mathf.Abs(
                centerDistance -
                movementRadius
            );


        float startDistance =
            Mathf.Max(
                minimumHorizontalDistanceFromSubmarine,
                avoidanceStartDistance
            );


        if (
            closestDistanceToCurrentOrbit >=
            startDistance
        )
        {
            return;
        }


        Vector3 directionFromSubmarineToCenter =
            centerPoint -
            submarinePosition;


        directionFromSubmarineToCenter.y =
            MinimumValue;


        if (
            directionFromSubmarineToCenter.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            directionFromSubmarineToCenter =
                shipRigidbody.position -
                submarinePosition;


            directionFromSubmarineToCenter.y =
                MinimumValue;
        }


        if (
            directionFromSubmarineToCenter.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            directionFromSubmarineToCenter =
                Vector3.right;
        }


        directionFromSubmarineToCenter.Normalize();


        float targetOrbitClearance =
            startDistance +
            avoidanceClearance;


        float targetCenterDistance;


        bool canKeepSubmarineInsideOrbit =
            movementRadius >
            targetOrbitClearance;


        if (
            centerDistance <
                movementRadius
            &&
            canKeepSubmarineInsideOrbit
        )
        {
            targetCenterDistance =
                movementRadius -
                targetOrbitClearance;
        }
        else
        {
            targetCenterDistance =
                movementRadius +
                targetOrbitClearance;
        }


        Vector3 targetCenterPoint =
            submarinePosition +
            directionFromSubmarineToCenter *
            targetCenterDistance;


        targetCenterPoint.y =
            centerPoint.y;


        // 一瞬で中心を切り替えず、
        // 毎FixedUpdate少しずつ新しい中心へ移動する
        centerPoint =
            Vector3.MoveTowards(
                centerPoint,
                targetCenterPoint,
                orbitCenterShiftSpeed *
                Time.fixedDeltaTime
            );
    }


    // ============================================================
    // 最低距離の最終安全処理
    // ============================================================

    private void ApplyEmergencyMinimumDistance(
        ref Vector3 nextPosition
    )
    {
        if (
            minimumHorizontalDistanceFromSubmarine <=
            MinimumValue
        )
        {
            return;
        }


        Vector3 submarinePosition =
            DataManager
                .GetSubmarinePosition();


        float nextDistance =
            CalculateHorizontalDistance(
                nextPosition,
                submarinePosition
            );


        if (
            nextDistance >=
            minimumHorizontalDistanceFromSubmarine
        )
        {
            return;
        }


        Vector3 awayFromSubmarine =
            nextPosition -
            submarinePosition;


        awayFromSubmarine.y =
            MinimumValue;


        if (
            awayFromSubmarine.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            awayFromSubmarine =
                shipRigidbody.position -
                submarinePosition;


            awayFromSubmarine.y =
                MinimumValue;
        }


        if (
            awayFromSubmarine.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            awayFromSubmarine =
                centerPoint -
                submarinePosition;


            awayFromSubmarine.y =
                MinimumValue;
        }


        if (
            awayFromSubmarine.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            awayFromSubmarine =
                Vector3.right;
        }


        awayFromSubmarine.Normalize();


        float targetDistance =
            minimumHorizontalDistanceFromSubmarine +
            avoidanceClearance;


        Vector3 safePosition =
            submarinePosition +
            awayFromSubmarine *
            targetDistance;


        safePosition.y =
            nextPosition.y;


        Vector3 correction =
            safePosition -
            nextPosition;


        nextPosition +=
            correction;


        // 次フレームに元の円へ戻ろうとしないよう、
        // 新しい周回中心も同じ量だけ移動する
        centerPoint +=
            correction;


        if (debugLog)
        {
            Debug.Log(
                gameObject.name +
                " が最低安全距離内へ入ったため、" +
                "周回中心を保ったまま最終安全補正を行いました。"
            );
        }
    }


    // ============================================================
    // XZ平面上の距離
    // ============================================================

    private float CalculateHorizontalDistance(
        Vector3 firstPosition,
        Vector3 secondPosition
    )
    {
        float deltaX =
            firstPosition.x -
            secondPosition.x;


        float deltaZ =
            firstPosition.z -
            secondPosition.z;


        return
            Mathf.Sqrt(
                deltaX * deltaX +
                deltaZ * deltaZ
            );
    }


    private Vector3 GetRadialOffset(
        float angle
    )
    {
        return
            new Vector3(
                Mathf.Cos(angle) *
                movementRadius,

                0.0f,

                Mathf.Sin(angle) *
                movementRadius
            );
    }


    // ============================================================
    // Observation
    // ============================================================

    private void UpdateObservation()
    {
        bool visible =
            IsVisibleFromPeriscope();


        if (!visible)
        {
            if (requireContinuousObservation)
            {
                observationTime =
                    0.0f;
            }


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


        BeginSignal();
    }


    private bool IsVisibleFromPeriscope()
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


        Vector3 forward =
            Quaternion.Euler(
                0.0f,
                periscopeYaw,
                0.0f
            )
            *
            Vector3.forward;


        Vector3 toEnemy =
            transform.position -
            periscopePosition;


        forward.y =
            0.0f;


        toEnemy.y =
            0.0f;


        if (
            toEnemy.magnitude >
            maximumDetectionDistance
        )
        {
            return false;
        }


        if (
            toEnemy.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return false;
        }


        float angle =
            Vector3.Angle(
                forward.normalized,
                toEnemy.normalized
            );


        return
            angle <=
            periscopeFOV *
            0.5f;
    }


    // ============================================================
    // Signal
    // ============================================================

    private void BeginSignal()
    {
        if (
            detected ||
            colorMemoryMissionManager ==
            null
        )
        {
            return;
        }


        bool accepted =
            colorMemoryMissionManager
                .TryBeginMission(
                    this,
                    sequence
                );


        if (!accepted)
        {
            observationTime =
                0.0f;

            return;
        }


        detected =
            true;


        signalStarted =
            true;


        if (shipVisual != null)
        {
            shipVisual.SetActive(
                true
            );
        }


        signalCoroutine =
            StartCoroutine(
                SignalRoutine()
            );
    }


    // ============================================================
    // Sequence generation
    // ============================================================

    private void GenerateSequence()
    {
        sequence.Clear();


        int minimum =
            Mathf.Max(
                1,
                minimumSequenceLength
            );


        int maximum =
            Mathf.Max(
                minimum,
                maximumSequenceLength
            );


        int length =
            Random.Range(
                minimum,
                maximum + 1
            );


        for (
            int i = 0;
            i < length;
            i++
        )
        {
            int random =
                Random.Range(
                    0,
                    3
                );


            switch (random)
            {
                case 0:

                    sequence.Add(
                        ColorSignalSymbol.Red
                    );

                    break;


                case 1:

                    sequence.Add(
                        ColorSignalSymbol.Blue
                    );

                    break;


                default:

                    sequence.Add(
                        ColorSignalSymbol.Yellow
                    );

                    break;
            }
        }


        if (debugLog)
        {
            Debug.Log(
                "Color sequence = " +
                SequenceToString()
            );
        }
    }


    // ============================================================
    // Light
    // ============================================================

    private void CreateSignalLight()
    {
        GameObject lightObject =
            new GameObject(
                "ColorMemorySignalLight"
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


        signalLight.intensity =
            signalIntensity;


        signalLight.range =
            signalRange;


        signalLight.enabled =
            false;
    }


    // ============================================================
    // Routine
    // ============================================================

    private IEnumerator SignalRoutine()
    {
        int repeatCount =
            Mathf.Max(
                1,
                sequenceRepeatCount
            );


        for (
            int repeat = 0;
            repeat < repeatCount;
            repeat++
        )
        {
            yield return
                Flash(
                    startMarkerColor,
                    startMarkerDuration,
                    startMarkerBlankDuration
                );


            for (
                int i = 0;
                i < sequence.Count;
                i++
            )
            {
                yield return
                    Flash(
                        GetColor(
                            sequence[i]
                        ),
                        colorDuration,
                        colorBlankDuration
                    );
            }


            if (
                repeat <
                repeatCount - 1
            )
            {
                yield return
                    new WaitForSecondsRealtime(
                        repeatInterval
                    );
            }
        }


        signalStarted =
            false;


        signalFinished =
            true;


        signalCoroutine =
            null;


        if (signalLight != null)
        {
            signalLight.enabled =
                false;
        }


        colorMemoryMissionManager
            .NotifyEnemySequenceFinished(
                this
            );
    }


    private IEnumerator Flash(
        Color color,
        float duration,
        float blankDuration
    )
    {
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


    // ============================================================
    // Color
    // ============================================================

    private Color GetColor(
        ColorSignalSymbol symbol
    )
    {
        switch (symbol)
        {
            case ColorSignalSymbol.Red:
                return redColor;

            case ColorSignalSymbol.Blue:
                return blueColor;

            case ColorSignalSymbol.Yellow:
                return yellowColor;

            default:
                return Color.white;
        }
    }


    // ============================================================
    // Debug
    // ============================================================

    private string SequenceToString()
    {
        string value =
            string.Empty;


        for (
            int i = 0;
            i < sequence.Count;
            i++
        )
        {
            switch (sequence[i])
            {
                case ColorSignalSymbol.Red:
                    value += "赤";
                    break;

                case ColorSignalSymbol.Blue:
                    value += "青";
                    break;

                case ColorSignalSymbol.Yellow:
                    value += "黄";
                    break;
            }


            if (
                i <
                sequence.Count - 1
            )
            {
                value +=
                    " → ";
            }
        }


        return value;
    }


    // ============================================================
    // Getter
    // ============================================================

    public IReadOnlyList<ColorSignalSymbol>
        GetColorSequence()
    {
        return
            sequence;
    }


    // ============================================================
    // Validate
    // ============================================================

    private void OnValidate()
    {
        movementSpeed =
            Mathf.Max(
                MinimumValue,
                movementSpeed
            );


        maximumDetectionDistance =
            Mathf.Max(
                MinimumValue,
                maximumDetectionDistance
            );


        requiredObservationTime =
            Mathf.Max(
                MinimumValue,
                requiredObservationTime
            );


        minimumSequenceLength =
            Mathf.Max(
                1,
                minimumSequenceLength
            );


        maximumSequenceLength =
            Mathf.Max(
                minimumSequenceLength,
                maximumSequenceLength
            );


        sequenceRepeatCount =
            Mathf.Max(
                1,
                sequenceRepeatCount
            );


        minimumHorizontalDistanceFromSubmarine =
            Mathf.Max(
                MinimumValue,
                minimumHorizontalDistanceFromSubmarine
            );


        avoidanceStartDistance =
            Mathf.Max(
                minimumHorizontalDistanceFromSubmarine,
                avoidanceStartDistance
            );


        orbitCenterShiftSpeed =
            Mathf.Max(
                MinimumValue,
                orbitCenterShiftSpeed
            );


        avoidanceClearance =
            Mathf.Max(
                MinimumValue,
                avoidanceClearance
            );
    }
}