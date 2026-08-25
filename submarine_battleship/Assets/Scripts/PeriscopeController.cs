using UnityEngine;

[DisallowMultipleComponent]
public class PeriscopeController : MonoBehaviour
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultRotationSmoothTime = 0.03f;
    private const float DefaultYawScale = 1.0f;

    private const float DefaultVerticalMoveSpeed = 10.0f;

    private const float DefaultMinimumLocalY = -2.0f;
    private const float DefaultMaximumLocalY = 2.0f;

    private const float DefaultWaterSurfaceWorldY = 0.0f;

    private const float DefaultPositionTolerance = 0.01f;

    private const float DefaultMaximumAcceptedYawDelta = 90.0f;

    private const float MinimumNonNegativeValue = 0.0f;

    private const float MinimumYawDeltaLimit = 1.0f;
    private const float MaximumYawDeltaLimit = 180.0f;

    private const int ButtonReleased = 0;
    private const int ButtonPressed = 1;

    private const int VerticalDirectionNone = 0;
    private const int VerticalDirectionUp = 1;
    private const int VerticalDirectionDown = -1;


    // ============================================================
    // Inspector設定
    // ============================================================

    [Header("References")]

    [SerializeField, Tooltip(
        "実際に回転・上下移動させる潜望鏡のTransform。" +
        "未設定の場合はこのGameObject自身を使用する")]
    private Transform periscopeTransform;


    [SerializeField, Tooltip(
        "潜望鏡の視点位置。" +
        "通常はMain Cameraを設定する。" +
        "未設定の場合はPeriscope Transformを使用する")]
    private Transform viewPointTransform;


    // ============================================================
    // Yaw設定
    // ============================================================

    [Header("Yaw Rotation")]

    [SerializeField, Tooltip(
        "センサーYawに対する回転倍率")]
    [Min(MinimumNonNegativeValue)]
    private float yawScale =
        DefaultYawScale;


    [SerializeField, Tooltip(
        "ONにするとセンサーYawの回転方向を反転する")]
    private bool invertYaw = false;


    [SerializeField, Tooltip(
        "潜望鏡回転を滑らかに追従させる時間。" +
        "0にすると補間を行わない")]
    [Min(MinimumNonNegativeValue)]
    private float rotationSmoothTime =
        DefaultRotationSmoothTime;


    [SerializeField, Tooltip(
        "ONの場合、1フレームで極端に大きく変化したYaw値を異常値として無視する")]
    private bool rejectAbnormalYawDelta = true;


    [SerializeField, Tooltip(
        "1フレームで許容する最大Yaw変化量")]
    [Range(
        MinimumYawDeltaLimit,
        MaximumYawDeltaLimit
    )]
    private float maximumAcceptedYawDelta =
        DefaultMaximumAcceptedYawDelta;


    // ============================================================
    // 上下移動設定
    // ============================================================

    [Header("Vertical Movement")]

    [SerializeField, Tooltip(
        "潜望鏡を最も下げたときのLocal Y")]
    private float minimumLocalY =
        DefaultMinimumLocalY;


    [SerializeField, Tooltip(
        "潜望鏡を最も上げたときのLocal Y")]
    private float maximumLocalY =
        DefaultMaximumLocalY;


    [SerializeField, Tooltip(
        "潜望鏡の上下移動速度")]
    [Min(MinimumNonNegativeValue)]
    private float verticalMoveSpeed =
        DefaultVerticalMoveSpeed;


    [SerializeField, Tooltip(
        "最大・最小位置に到達したと判定するための許容誤差")]
    [Min(MinimumNonNegativeValue)]
    private float positionTolerance =
        DefaultPositionTolerance;


    // ============================================================
    // 海面設定
    // ============================================================

    [Header("Water Surface")]

    [SerializeField, Tooltip(
        "ゲーム世界における海面のWorld Y座標")]
    private float waterSurfaceWorldY =
        DefaultWaterSurfaceWorldY;


    // ============================================================
    // Yaw内部状態
    // ============================================================

    private bool yawReferenceInitialized = false;

    private float previousSensorYaw;

    // センサーが開始時から合計何度回転したか
    // 360°を超えても保持する
    private float accumulatedSensorYaw = 0.0f;

    private Vector3 baseLocalEulerAngles;

    private float targetLocalYaw;
    private float currentLocalYaw;
    private float yawVelocity;


    // ============================================================
    // Reset
    // ============================================================

    private void Reset()
    {
        periscopeTransform =
            transform;


        Camera childCamera =
            GetComponentInChildren<Camera>();


        if (childCamera != null)
        {
            viewPointTransform =
                childCamera.transform;
        }
    }


    // ============================================================
    // Awake
    // ============================================================

    private void Awake()
    {
        ResolveReferences();

        ValidateSettings();
    }


    // ============================================================
    // Start
    // ============================================================

    private void Start()
    {
        if (periscopeTransform == null)
        {
            Debug.LogError(
                "PeriscopeControllerで潜望鏡のTransformを取得できません。"
            );

            enabled = false;

            return;
        }


        // =========================
        // Yaw初期状態
        // =========================

        baseLocalEulerAngles =
            periscopeTransform.localEulerAngles;


        currentLocalYaw =
            baseLocalEulerAngles.y;


        targetLocalYaw =
            currentLocalYaw;


        yawVelocity =
            0.0f;


        // 最初のLateUpdateで現在のセンサーYawを
        // 基準角として取得する
        yawReferenceInitialized =
            false;


        // =========================
        // 高さを有効範囲内へ
        // =========================

        ClampCurrentLocalHeight();


        // =========================
        // 初期状態保存
        // =========================

        UpdateDataManager();
    }


    // ============================================================
    // LateUpdate
    // ============================================================

    /// <summary>
    /// GameManager.Update()でSensorReadの値がDataManagerへ
    /// 保存された後に処理できるようLateUpdateを使用する。
    /// </summary>
    private void LateUpdate()
    {
        if (periscopeTransform == null)
        {
            return;
        }


        UpdateYawRotation();

        UpdateVerticalMovement();

        UpdateDataManager();
    }


    // ============================================================
    // 参照取得
    // ============================================================

    private void ResolveReferences()
    {
        if (periscopeTransform == null)
        {
            periscopeTransform =
                transform;
        }


        if (viewPointTransform == null)
        {
            Camera childCamera =
                periscopeTransform
                    .GetComponentInChildren<Camera>();


            if (childCamera != null)
            {
                viewPointTransform =
                    childCamera.transform;
            }
            else
            {
                viewPointTransform =
                    periscopeTransform;
            }
        }
    }


    // ============================================================
    // Yaw回転
    // ============================================================

    private void UpdateYawRotation()
    {
        float currentSensorYaw =
            DataManager.GetSensorYaw();


        // =========================
        // 最初のYawを基準角にする
        // =========================

        if (!yawReferenceInitialized)
        {
            previousSensorYaw =
                currentSensorYaw;

            yawReferenceInitialized =
                true;

            return;
        }


        // =========================
        // 前回からのYaw変化量
        // =========================

        float sensorYawDelta =
            Mathf.DeltaAngle(
                previousSensorYaw,
                currentSensorYaw
            );


        // 次回計算用に必ず保存
        previousSensorYaw =
            currentSensorYaw;


        // =========================
        // 異常値除外
        // =========================

        if (
            rejectAbnormalYawDelta &&
            Mathf.Abs(sensorYawDelta) >
            maximumAcceptedYawDelta
        )
        {
            return;
        }


        // =========================
        // 回転量を累積
        // =========================

        accumulatedSensorYaw +=
            sensorYawDelta;


        float directionMultiplier =
            invertYaw
                ? -1.0f
                : 1.0f;


        targetLocalYaw =
            baseLocalEulerAngles.y +
            accumulatedSensorYaw *
            yawScale *
            directionMultiplier;


        // =========================
        // 回転補間
        // =========================

        if (
            rotationSmoothTime <=
            MinimumNonNegativeValue
        )
        {
            currentLocalYaw =
                targetLocalYaw;
        }
        else
        {
            currentLocalYaw =
                Mathf.SmoothDampAngle(
                    currentLocalYaw,
                    targetLocalYaw,
                    ref yawVelocity,
                    rotationSmoothTime
                );
        }


        ApplyLocalYaw();
    }


    // ============================================================
    // Yaw適用
    // ============================================================

    private void ApplyLocalYaw()
    {
        Vector3 localEulerAngles =
            baseLocalEulerAngles;


        localEulerAngles.y =
            currentLocalYaw;


        periscopeTransform.localRotation =
            Quaternion.Euler(
                localEulerAngles
            );
    }


    // ============================================================
    // 上下移動
    // ============================================================

    private void UpdateVerticalMovement()
    {
        int verticalDirection =
            GetVerticalInput();


        if (
            verticalDirection ==
            VerticalDirectionNone
        )
        {
            return;
        }


        Vector3 localPosition =
            periscopeTransform.localPosition;


        float movement =
            verticalDirection *
            verticalMoveSpeed *
            Time.deltaTime;


        localPosition.y =
            Mathf.Clamp(
                localPosition.y +
                movement,
                minimumLocalY,
                maximumLocalY
            );


        periscopeTransform.localPosition =
            localPosition;
    }


    // ============================================================
    // 上下入力
    // ============================================================

    private int GetVerticalInput()
    {
        int buttonUp =
            DataManager.GetSensorButton2();


        int buttonDown =
            DataManager.GetSensorButton3();


        bool upPressed =
            buttonUp ==
            ButtonPressed;


        bool downPressed =
            buttonDown ==
            ButtonPressed;


        // Button2だけ
        if (
            upPressed &&
            !downPressed
        )
        {
            return
                VerticalDirectionUp;
        }


        // Button3だけ
        if (
            !upPressed &&
            downPressed
        )
        {
            return
                VerticalDirectionDown;
        }


        // 両方押下、または両方未押下
        return
            VerticalDirectionNone;
    }


    // ============================================================
    // 高さ制限
    // ============================================================

    private void ClampCurrentLocalHeight()
    {
        if (periscopeTransform == null)
        {
            return;
        }


        Vector3 localPosition =
            periscopeTransform.localPosition;


        localPosition.y =
            Mathf.Clamp(
                localPosition.y,
                minimumLocalY,
                maximumLocalY
            );


        periscopeTransform.localPosition =
            localPosition;
    }


    // ============================================================
    // DataManager更新
    // ============================================================

    private void UpdateDataManager()
    {
        if (periscopeTransform == null)
        {
            return;
        }


        Transform actualViewPoint =
            viewPointTransform != null
                ? viewPointTransform
                : periscopeTransform;


        // =========================
        // 位置・向き
        // =========================

        DataManager.SetPeriscopePosition(
            actualViewPoint.position
        );


        DataManager.SetPeriscopeRotation(
            actualViewPoint.eulerAngles.y
        );


        DataManager.SetPeriscopeLocalHeight(
            periscopeTransform.localPosition.y
        );


        // =========================
        // 海上に露出しているか
        // =========================

        bool isAboveSurface =
            actualViewPoint.position.y >
            waterSurfaceWorldY;


        DataManager.SetIsPeriscopeAboveSurface(
            isAboveSurface
        );


        // =========================
        // 完全に上がっているか
        // =========================

        bool isFullyRaised =
            periscopeTransform.localPosition.y >=
            maximumLocalY -
            positionTolerance;


        DataManager.SetIsPeriscopeFullyRaised(
            isFullyRaised
        );


        // =========================
        // 完全に下がっているか
        // =========================

        bool isFullyLowered =
            periscopeTransform.localPosition.y <=
            minimumLocalY +
            positionTolerance;


        DataManager.SetIsPeriscopeFullyLowered(
            isFullyLowered
        );
    }


    // ============================================================
    // Yaw再基準化
    // ============================================================

    /// <summary>
    /// 現在の実際の潜望鏡の向きを維持したまま、
    /// 現在のセンサーYawを新しい基準値にする。
    ///
    /// 将来的に管理者用の
    /// 「潜望鏡角度再調整」機能から呼び出せる。
    /// </summary>
    public void RecenterYaw()
    {
        if (periscopeTransform == null)
        {
            return;
        }


        baseLocalEulerAngles =
            periscopeTransform.localEulerAngles;


        currentLocalYaw =
            baseLocalEulerAngles.y;


        targetLocalYaw =
            currentLocalYaw;


        accumulatedSensorYaw =
            0.0f;


        yawVelocity =
            0.0f;


        previousSensorYaw =
            DataManager.GetSensorYaw();


        yawReferenceInitialized =
            true;
    }


    // ============================================================
    // 高さを外部から変更
    // ============================================================

    /// <summary>
    /// 潜望鏡のLocal Yを外部から設定する。
    ///
    /// 管理者操作や、将来作り直すチュートリアルなどから
    /// 使用できる。
    /// </summary>
    public bool SetLocalHeight(
        float localY
    )
    {
        if (
            float.IsNaN(localY) ||
            float.IsInfinity(localY)
        )
        {
            return false;
        }


        if (periscopeTransform == null)
        {
            return false;
        }


        Vector3 localPosition =
            periscopeTransform.localPosition;


        localPosition.y =
            Mathf.Clamp(
                localY,
                minimumLocalY,
                maximumLocalY
            );


        periscopeTransform.localPosition =
            localPosition;


        UpdateDataManager();


        return true;
    }


    // ============================================================
    // 状態取得
    // ============================================================

    public bool GetIsAboveSurface()
    {
        return
            DataManager
                .GetIsPeriscopeAboveSurface();
    }


    public bool GetIsFullyRaised()
    {
        return
            DataManager
                .GetIsPeriscopeFullyRaised();
    }


    public bool GetIsFullyLowered()
    {
        return
            DataManager
                .GetIsPeriscopeFullyLowered();
    }


    // ============================================================
    // Inspector値検証
    // ============================================================

    private void OnValidate()
    {
        ValidateSettings();
    }


    private void ValidateSettings()
    {
        yawScale =
            Mathf.Max(
                MinimumNonNegativeValue,
                yawScale
            );


        rotationSmoothTime =
            Mathf.Max(
                MinimumNonNegativeValue,
                rotationSmoothTime
            );


        verticalMoveSpeed =
            Mathf.Max(
                MinimumNonNegativeValue,
                verticalMoveSpeed
            );


        positionTolerance =
            Mathf.Max(
                MinimumNonNegativeValue,
                positionTolerance
            );


        maximumAcceptedYawDelta =
            Mathf.Clamp(
                maximumAcceptedYawDelta,
                MinimumYawDeltaLimit,
                MaximumYawDeltaLimit
            );


        // 最大値と最小値が逆なら入れ替える
        if (
            maximumLocalY <
            minimumLocalY
        )
        {
            float temporaryValue =
                minimumLocalY;


            minimumLocalY =
                maximumLocalY;


            maximumLocalY =
                temporaryValue;
        }
    }
}