using UnityEngine;

public class Submarine : Ship
{
    [SerializeField] private float submarine_speed;


    // =========================
    // センサー回転設定
    // =========================

    [Header("Sensor Rotation")]

    private float rotationSmoothTime = 0.03f;

    private float yawSign = 1f;
    private float yawScale = 1f;


    // =========================
    // 上下移動設定
    // =========================

    [Header("Vertical Movement")]

    // Y座標の最大値
    private float submarine_position_y_max = 2f;

    // Y座標の最小値
    private float submarine_position_y_min = -2f;

    // 上下移動の速さ
    private float verticalMoveSpeed = 10f;


    // =========================
    // Yaw制御
    // =========================

    private float startSensorYaw;
    private float startUnityYaw;

    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;


    // =========================
    // Awake
    // =========================

    void Awake()
    {
        if (submarine_speed < 0)
        {
            submarine_speed = 1f;
        }


        if (verticalMoveSpeed < 0)
        {
            verticalMoveSpeed = 1f;
        }


        // 最大値と最小値が逆に設定されていた場合の対策
        if (
            submarine_position_y_max <
            submarine_position_y_min
        )
        {
            float temp =
                submarine_position_y_max;

            submarine_position_y_max =
                submarine_position_y_min;

            submarine_position_y_min =
                temp;
        }
    }


    // =========================
    // Start
    // =========================

    protected override void Start()
    {
        base.Start();


        this.max_speed =
            DataManager.GetSubmarineMaxSpeed();


        speed = submarine_speed;


        if (rigidbody != null)
        {
            rigidbody.interpolation =
                RigidbodyInterpolation.Interpolate;
        }


        // センサーYawの初期値
        startSensorYaw =
            DataManager.GetSensorYaw();


        startUnityYaw =
            transform.eulerAngles.y;


        currentYaw =
            startUnityYaw;


        targetYaw =
            startUnityYaw;
    }


    // =========================
    // Update
    // =========================

    protected override void Update()
    {
        base.Update();


        // 現在位置をDataManagerに保存
        DataManager.SetSubmarinePosition(
            transform.position
        );


        // 現在角度をDataManagerに保存
        DataManager.SetSubmarineRotation(
            transform.eulerAngles.y
        );


        // =========================
        // センサーYaw取得
        // =========================

        float sensorYaw =
            DataManager.GetSensorYaw();


        // 開始時からセンサが何度回ったか
        float sensorDeltaYaw =
            Mathf.DeltaAngle(
                startSensorYaw,
                sensorYaw
            );


        // Unity側の目標角度
        targetYaw =
            startUnityYaw
            +
            sensorDeltaYaw
            *
            yawSign
            *
            yawScale;
    }


    // =========================
    // FixedUpdate
    // =========================

    protected override void FixedUpdate()
    {
        if (rigidbody == null)
        {
            return;
        }


        // =========================
        // 回転
        // =========================

        currentYaw =
            Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                rotationSmoothTime,
                Mathf.Infinity,
                Time.fixedDeltaTime
            );


        rigidbody.MoveRotation(
            Quaternion.Euler(
                0f,
                currentYaw,
                0f
            )
        );


        // =========================
        // 移動
        // =========================

        Vector3 nextPosition =
            CalculateNextPosition();


        rigidbody.MovePosition(
            nextPosition
        );


        // =========================
        // DataManager更新
        // =========================

        DataManager.SetSubmarinePosition(
            nextPosition
        );


        DataManager.SetSubmarineRotation(
            currentYaw
        );
    }


    // =========================
    // 次の座標を計算
    // =========================

    private Vector3 CalculateNextPosition()
    {
        Vector3 currentPosition =
            rigidbody.position;


        // =========================
        // 前進移動
        // =========================

        Vector3 forwardMove =
            transform.forward
            *
            speed
            *
            Time.fixedDeltaTime;


        // =========================
        // 上下移動
        // =========================

        int verticalInput =
            GetVerticalInput();


        float nextY =
            currentPosition.y
            +
            verticalInput
            *
            verticalMoveSpeed
            *
            Time.fixedDeltaTime;


        // Y座標を範囲内に制限
        nextY =
            Mathf.Clamp(
                nextY,
                submarine_position_y_min,
                submarine_position_y_max
            );


        // =========================
        // 最終位置
        // =========================

        Vector3 nextPosition =
            currentPosition
            +
            forwardMove;


        nextPosition.y =
            nextY;


        return nextPosition;
    }


    // =========================
    // 上下移動入力
    // =========================

    private int GetVerticalInput()
    {
        // Button2：上昇
        int buttonUp =
            DataManager.GetSensorButton2();


        // Button3：下降
        int buttonDown =
            DataManager.GetSensorButton3();


        // =========================
        // Button2だけ押されている
        // → 上昇
        // =========================

        if (
            buttonUp == 1 &&
            buttonDown == 0
        )
        {
            return 1;
        }


        // =========================
        // Button3だけ押されている
        // → 下降
        // =========================

        if (
            buttonUp == 0 &&
            buttonDown == 1
        )
        {
            return -1;
        }


        // =========================
        // 両方押している
        // または両方押していない
        // → 停止
        // =========================

        return 0;
    }
}