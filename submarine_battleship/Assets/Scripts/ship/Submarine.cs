using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Submarine : Ship
{
    // ============================================================
    // 定数
    // ============================================================

    private const float DefaultSubmarineSpeed = 1.0f;
    private const float MinimumSpeed = 0.0f;


    // ============================================================
    // Inspector設定
    // ============================================================

    [Header("Automatic Navigation")]

    [SerializeField, Tooltip("潜水艦が自動航行するときの前進速度")]
    [Min(MinimumSpeed)]
    private float submarineSpeed = DefaultSubmarineSpeed;


    [SerializeField, Tooltip(
        "ONの場合、DataManagerで設定されている潜水艦の最大速度を超えないようにする")]
    private bool clampToMaximumSpeed = true;


    [SerializeField, Tooltip(
        "潜水艦を自動で前進させるかどうか")]
    private bool automaticForwardMovementEnabled = true;


    [Header("Rigidbody")]

    [SerializeField, Tooltip(
        "RigidbodyのInterpolationを有効にするか")]
    private bool useInterpolation = true;


    // ============================================================
    // Start
    // ============================================================

    protected override void Start()
    {
        base.Start();


        if (rigidbody == null)
        {
            Debug.LogError(
                "SubmarineにRigidbodyが設定されていません。"
            );

            enabled = false;

            return;
        }


        // =========================
        // 最大速度
        // =========================

        max_speed =
            DataManager.GetSubmarineMaxSpeed();


        // =========================
        // 初期速度
        // =========================

        speed =
            GetValidatedSpeed(
                submarineSpeed
            );


        // =========================
        // Rigidbody補間
        // =========================

        rigidbody.interpolation =
            useInterpolation
                ? RigidbodyInterpolation.Interpolate
                : RigidbodyInterpolation.None;


        // =========================
        // 初期状態をDataManagerへ保存
        // =========================

        UpdateDataManager();
    }


    // ============================================================
    // FixedUpdate
    // ============================================================

    protected override void FixedUpdate()
    {
        if (rigidbody == null)
        {
            return;
        }


        // =========================
        // 自動前進
        // =========================

        if (
            automaticForwardMovementEnabled &&
            speed > MinimumSpeed
        )
        {
            Vector3 nextPosition =
                CalculateNextPosition();

            rigidbody.MovePosition(
                nextPosition
            );
        }


        // =========================
        // DataManager更新
        // =========================

        UpdateDataManager();
    }


    // ============================================================
    // 次の位置を計算
    // ============================================================

    /// <summary>
    /// 現在の潜水艦の向きを基準に、
    /// 次の物理フレームでの位置を計算する。
    ///
    /// 潜水艦のPitchやRollが変化した場合でも、
    /// 自動航行では水平面上を前進する。
    /// </summary>
    private Vector3 CalculateNextPosition()
    {
        Vector3 currentPosition =
            rigidbody.position;


        // Rigidbodyが向いている前方方向
        Vector3 forwardDirection =
            rigidbody.rotation *
            Vector3.forward;


        // 水平方向だけを使用する
        forwardDirection =
            Vector3.ProjectOnPlane(
                forwardDirection,
                Vector3.up
            );


        // 前方方向を取得できない場合は移動しない
        if (
            forwardDirection.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return currentPosition;
        }


        forwardDirection.Normalize();


        Vector3 movement =
            forwardDirection *
            speed *
            Time.fixedDeltaTime;


        return
            currentPosition +
            movement;
    }


    // ============================================================
    // DataManager更新
    // ============================================================

    /// <summary>
    /// 潜水艦本体の位置・向きをDataManagerへ保存する。
    ///
    /// ここで保存する向きは、
    /// 潜望鏡の向きではなく潜水艦本体の向き。
    /// </summary>
    private void UpdateDataManager()
    {
        if (rigidbody != null)
        {
            DataManager.SetSubmarinePosition(
                rigidbody.position
            );

            DataManager.SetSubmarineRotation(
                rigidbody.rotation.eulerAngles.y
            );

            return;
        }


        // Rigidbodyが取得できない場合の予備処理
        DataManager.SetSubmarinePosition(
            transform.position
        );

        DataManager.SetSubmarineRotation(
            transform.eulerAngles.y
        );
    }


    // ============================================================
    // 速度設定
    // ============================================================

    /// <summary>
    /// 潜水艦の前進速度を変更する。
    /// </summary>
    public bool SetSpeed(
        float newSpeed
    )
    {
        if (
            float.IsNaN(newSpeed) ||
            float.IsInfinity(newSpeed) ||
            newSpeed < MinimumSpeed
        )
        {
            Debug.LogError(
                "潜水艦の速度として無効な値です: " +
                newSpeed
            );

            return false;
        }


        submarineSpeed =
            newSpeed;

        speed =
            GetValidatedSpeed(
                submarineSpeed
            );


        return true;
    }


    /// <summary>
    /// 現在の前進速度を取得する。
    /// </summary>
    public float GetSpeed()
    {
        return speed;
    }


    /// <summary>
    /// 自動前進のON/OFFを切り替える。
    ///
    /// 将来的にイベントやゲームオーバーなどで
    /// 潜水艦を停止するときに利用できる。
    /// </summary>
    public void SetAutomaticForwardMovementEnabled(
        bool enabled
    )
    {
        automaticForwardMovementEnabled =
            enabled;
    }


    /// <summary>
    /// 自動前進が有効かどうか。
    /// </summary>
    public bool GetAutomaticForwardMovementEnabled()
    {
        return automaticForwardMovementEnabled;
    }


    // ============================================================
    // 速度検証
    // ============================================================

    private float GetValidatedSpeed(
        float requestedSpeed
    )
    {
        float validatedSpeed =
            Mathf.Max(
                MinimumSpeed,
                requestedSpeed
            );


        if (!clampToMaximumSpeed)
        {
            return validatedSpeed;
        }


        return
            Mathf.Min(
                validatedSpeed,
                max_speed
            );
    }


    // ============================================================
    // Inspector値検証
    // ============================================================

    private void OnValidate()
    {
        submarineSpeed =
            Mathf.Max(
                MinimumSpeed,
                submarineSpeed
            );
    }
}