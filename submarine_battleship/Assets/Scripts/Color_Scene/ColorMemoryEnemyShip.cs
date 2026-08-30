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


        movementAngle +=
            movementSpeed *
            Time.fixedDeltaTime;


        movementAngle =
            Mathf.Repeat(
                movementAngle,
                FullCircleRadians
            );


        Vector3 nextPosition =
            centerPoint +
            GetRadialOffset(
                movementAngle
            );


        Vector3 direction =
            nextPosition -
            shipRigidbody.position;


        direction.y =
            0.0f;


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
                    0.0f,
                    modelRotationOffset,
                    0.0f
                );


            shipRigidbody.MoveRotation(
                rotation
            );
        }


        shipRigidbody.MovePosition(
            nextPosition
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
    }
}