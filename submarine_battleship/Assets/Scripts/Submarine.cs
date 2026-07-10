using UnityEngine;

public class Submarine : Ship
{
    [SerializeField] private float submarine_speed;

    [Header("Sensor")]
    [SerializeField, Tooltip("センサー読み取り用スクリプト")]
    private SensorRead sensorRead;

    [Header("Sensor Rotation")]
    private float rotationSmoothTime = 0.03f;
    private float yawSign = 1f;
    private float yawScale = 1f;

    [Header("Vertical Movement")]
    // [SerializeField, Tooltip("潜水艦のY座標の最大値")]
    private float submarine_position_y_max = 2f;

    // [SerializeField, Tooltip("潜水艦のY座標の最小値")]
    private float submarine_position_y_min = -2f;

    // [SerializeField, Tooltip("上下移動の速さ")]
    private float verticalMoveSpeed = 10f;

    private float startSensorYaw;
    private float startUnityYaw;

    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;

    void Awake()
    {
        if (submarine_speed < 0) submarine_speed = 1f;
        if (verticalMoveSpeed < 0) verticalMoveSpeed = 1f;

        // 最大値と最小値が逆に設定されていた場合の対策
        if (submarine_position_y_max < submarine_position_y_min)
        {
            float temp = submarine_position_y_max;
            submarine_position_y_max = submarine_position_y_min;
            submarine_position_y_min = temp;
        }
    }

    protected override void Start()
    {
        base.Start();

        this.max_speed = DataManager.GetSubmarineMaxSpeed();
        speed = submarine_speed;

        if (rigidbody != null)
        {
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        startSensorYaw = DataManager.GetSensorYaw();
        startUnityYaw = transform.eulerAngles.y;

        currentYaw = startUnityYaw;
        targetYaw = startUnityYaw;
    }

    protected override void Update()
    {
        base.Update();

        DataManager.SetSubmarinePosition(transform.position);
        DataManager.SetSubmarineRotation(transform.eulerAngles.y);

        float sensorYaw = DataManager.GetSensorYaw();

        // 開始時からセンサが何度回ったか
        float sensorDeltaYaw = Mathf.DeltaAngle(startSensorYaw, sensorYaw);

        // Unity側の目標角度
        targetYaw = startUnityYaw + sensorDeltaYaw * yawSign * yawScale;
    }

    protected override void FixedUpdate()
    {
        if (rigidbody == null) return;

        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        rigidbody.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));

        Vector3 nextPosition = CalculateNextPosition();

        rigidbody.MovePosition(nextPosition);

        DataManager.SetSubmarinePosition(nextPosition);
        DataManager.SetSubmarineRotation(currentYaw);
    }

    private Vector3 CalculateNextPosition()
    {
        Vector3 currentPosition = rigidbody.position;

        // 前進移動
        Vector3 forwardMove = transform.forward * speed * Time.fixedDeltaTime;

        // 上下移動
        int encode = GetEncodeValue();

        float nextY = currentPosition.y + encode * verticalMoveSpeed * Time.fixedDeltaTime;

        nextY = Mathf.Clamp(
            nextY,
            submarine_position_y_min,
            submarine_position_y_max
        );

        Vector3 nextPosition = currentPosition + forwardMove;
        nextPosition.y = nextY;

        return nextPosition;
    }

    private int GetEncodeValue()
    {
        if (sensorRead == null) return 0;

        int encode = sensorRead.GetEncode();

        // 念のため -1, 0, 1 の範囲に制限
        return Mathf.Clamp(encode, -1, 1);
    }
}