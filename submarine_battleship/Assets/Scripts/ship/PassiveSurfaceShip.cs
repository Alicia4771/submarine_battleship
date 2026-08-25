using UnityEngine;

[RequireComponent(
    typeof(Rigidbody),
    typeof(SurfaceContact)
)]
[DisallowMultipleComponent]
public class PassiveSurfaceShip : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultMovementSpeed =
        0.15f;

    private const float DefaultMovementRadius =
        30.0f;

    private const float DefaultRadiusRandomFactor =
        0.1f;

    private const float DefaultModelRotationOffset =
        90.0f;

    private const float MinimumNonNegativeValue =
        0.0f;

    private const float RandomDirectionThreshold =
        0.5f;

    private const float ClockwiseDirection =
        -1.0f;

    private const float CounterClockwiseDirection =
        1.0f;


    private static readonly float FullCircleRadians =
        Mathf.PI *
        2.0f;


    // ============================================================
    // 艦種
    // ============================================================

    [Header("Contact")]

    [SerializeField, Tooltip(
        "この船の種類。" +
        "FriendlyまたはNeutralを設定する")]
    private SurfaceContactType contactType =
        SurfaceContactType.Friendly;


    // ============================================================
    // 移動
    // ============================================================

    [Header("Movement")]

    [SerializeField, Tooltip(
        "この船を移動させるか")]
    private bool movementEnabled =
        true;


    [SerializeField, Tooltip(
        "円運動するときの角速度")]
    [Min(MinimumNonNegativeValue)]
    private float movementSpeed =
        DefaultMovementSpeed;


    [SerializeField, Tooltip(
        "円運動の基準半径")]
    [Min(MinimumNonNegativeValue)]
    private float movementRadius =
        DefaultMovementRadius;


    [SerializeField, Tooltip(
        "各船の移動半径に加えるランダム幅。" +
        "0.1なら基準半径の±10%")]
    [Range(
        MinimumNonNegativeValue,
        1.0f
    )]
    private float radiusRandomFactor =
        DefaultRadiusRandomFactor;


    [SerializeField, Tooltip(
        "船モデルの正面方向を補正する角度")]
    private float modelRotationOffset =
        DefaultModelRotationOffset;


    [SerializeField, Tooltip(
        "円運動の方向を船ごとにランダムに決める")]
    private bool randomizeMovementDirection =
        true;


    [SerializeField, Tooltip(
        "Randomize Movement DirectionがOFFの時に、" +
        "時計回りで航行するか")]
    private bool clockwise =
        false;


    // ============================================================
    // Rigidbody
    // ============================================================

    [Header("Rigidbody")]

    [SerializeField, Tooltip(
        "船なので通常は重力を無効にする")]
    private bool disableGravity =
        true;


    // ============================================================
    // Debug
    // ============================================================

    [Header("Debug")]

    [SerializeField, Tooltip(
        "初期化情報をConsoleへ表示する")]
    private bool debugLog =
        false;


    // ============================================================
    // 内部状態
    // ============================================================

    private Rigidbody shipRigidbody;

    private SurfaceContact surfaceContact;


    private Vector3 movementCenter;


    private float actualMovementRadius =
        MinimumNonNegativeValue;


    private float currentMovementAngle =
        MinimumNonNegativeValue;


    private float movementDirection =
        CounterClockwiseDirection;


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveComponents();

        ConfigureRigidbody();

        ApplyContactType();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        InitializeMovement();
    }


    // ============================================================
    // FixedUpdate
    // ============================================================

    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            return;
        }


        if (shipRigidbody == null)
        {
            return;
        }


        UpdateCircularMovement();
    }


    // ============================================================
    // Component取得
    // ============================================================

    private void ResolveComponents()
    {
        shipRigidbody =
            GetComponent<Rigidbody>();


        surfaceContact =
            GetComponent<SurfaceContact>();
    }


    // ============================================================
    // Rigidbody設定
    // ============================================================

    private void ConfigureRigidbody()
    {
        if (shipRigidbody == null)
        {
            Debug.LogError(
                "PassiveSurfaceShipにRigidbodyがありません。"
            );

            enabled =
                false;

            return;
        }


        if (disableGravity)
        {
            shipRigidbody.useGravity =
                false;
        }


        shipRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;
    }


    // ============================================================
    // SurfaceContact設定
    // ============================================================

    private void ApplyContactType()
    {
        if (surfaceContact == null)
        {
            surfaceContact =
                GetComponent<SurfaceContact>();
        }


        if (surfaceContact == null)
        {
            Debug.LogError(
                "SurfaceContactが見つかりません。"
            );

            return;
        }


        if (!IsPassiveContactType(
            contactType
        ))
        {
            Debug.LogWarning(
                "PassiveSurfaceShipには" +
                "FriendlyまたはNeutralを設定してください。" +
                "Friendlyへ変更します。"
            );


            contactType =
                SurfaceContactType.Friendly;
        }


        surfaceContact.SetContactType(
            contactType
        );


        surfaceContact.SetSonarDetectable(
            true
        );
    }


    // ============================================================
    // 外部から艦種設定
    // ============================================================

    public bool ConfigureContactType(
        SurfaceContactType newType
    )
    {
        if (!IsPassiveContactType(
            newType
        ))
        {
            Debug.LogWarning(
                "PassiveSurfaceShipには" +
                "FriendlyまたはNeutralを設定してください。"
            );

            return false;
        }


        contactType =
            newType;


        ApplyContactType();


        return true;
    }


    // ============================================================
    // Passive艦種か
    // ============================================================

    private bool IsPassiveContactType(
        SurfaceContactType type
    )
    {
        return
            type ==
                SurfaceContactType.Friendly
            ||
            type ==
                SurfaceContactType.Neutral;
    }


    // ============================================================
    // 移動初期化
    // ============================================================

    private void InitializeMovement()
    {
        if (shipRigidbody == null)
        {
            return;
        }


        // =========================
        // 実際の移動半径
        // =========================

        float randomRadiusOffset =
            movementRadius *
            radiusRandomFactor;


        actualMovementRadius =
            movementRadius +
            Random.Range(
                -randomRadiusOffset,
                randomRadiusOffset
            );


        actualMovementRadius =
            Mathf.Max(
                MinimumNonNegativeValue,
                actualMovementRadius
            );


        // =========================
        // 円周上の初期角度
        // =========================

        currentMovementAngle =
            Random.Range(
                MinimumNonNegativeValue,
                FullCircleRadians
            );


        // =========================
        // 航行方向
        // =========================

        if (randomizeMovementDirection)
        {
            movementDirection =
                Random.value <
                RandomDirectionThreshold
                    ? ClockwiseDirection
                    : CounterClockwiseDirection;
        }
        else
        {
            movementDirection =
                clockwise
                    ? ClockwiseDirection
                    : CounterClockwiseDirection;
        }


        // =========================
        // 円の中心を逆算
        // =========================
        //
        // Spawnerが決めた位置を、
        // 円周上の初期位置として使用する。
        //
        // Startした瞬間に船が別の場所へ
        // 移動しないようにしている。
        // =========================

        Vector3 radialOffset =
            CalculateRadialOffset(
                currentMovementAngle
            );


        movementCenter =
            shipRigidbody.position -
            radialOffset;


        if (debugLog)
        {
            Debug.Log(
                gameObject.name +
                " / Type=" +
                contactType +
                " / Radius=" +
                actualMovementRadius +
                " / Direction=" +
                movementDirection
            );
        }
    }


    // ============================================================
    // 円運動
    // ============================================================

    private void UpdateCircularMovement()
    {
        if (
            actualMovementRadius <=
            Mathf.Epsilon
        )
        {
            return;
        }


        // =========================
        // 角度更新
        // =========================

        currentMovementAngle +=
            movementSpeed *
            movementDirection *
            Time.fixedDeltaTime;


        currentMovementAngle =
            Mathf.Repeat(
                currentMovementAngle,
                FullCircleRadians
            );


        // =========================
        // 次の位置
        // =========================

        Vector3 radialOffset =
            CalculateRadialOffset(
                currentMovementAngle
            );


        Vector3 nextPosition =
            movementCenter +
            radialOffset;


        // =========================
        // 航行方向
        // =========================

        Vector3 moveDirection =
            nextPosition -
            shipRigidbody.position;


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
        // 移動
        // =========================

        shipRigidbody.MovePosition(
            nextPosition
        );
    }


    // ============================================================
    // 円周上のオフセット
    // ============================================================

    private Vector3 CalculateRadialOffset(
        float angle
    )
    {
        float x =
            Mathf.Cos(
                angle
            ) *
            actualMovementRadius;


        float z =
            Mathf.Sin(
                angle
            ) *
            actualMovementRadius;


        return
            new Vector3(
                x,
                MinimumNonNegativeValue,
                z
            );
    }


    // ============================================================
    // Getter
    // ============================================================

    public SurfaceContactType GetContactType()
    {
        return
            contactType;
    }


    public Vector3 GetMovementCenter()
    {
        return
            movementCenter;
    }


    public float GetMovementRadius()
    {
        return
            actualMovementRadius;
    }


    public bool GetMovementEnabled()
    {
        return
            movementEnabled;
    }


    // ============================================================
    // Setter
    // ============================================================

    public void SetMovementEnabled(
        bool enabled
    )
    {
        movementEnabled =
            enabled;
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


        movementRadius =
            Mathf.Max(
                MinimumNonNegativeValue,
                movementRadius
            );


        radiusRandomFactor =
            Mathf.Clamp01(
                radiusRandomFactor
            );


        if (
            contactType !=
                SurfaceContactType.Friendly
            &&
            contactType !=
                SurfaceContactType.Neutral
        )
        {
            contactType =
                SurfaceContactType.Friendly;
        }
    }
}