using UnityEngine;

public class Submarine : Ship
{
    [SerializeField] private float submarine_speed;

    [Header("Sensor Rotation")]
    // [SerializeField, Tooltip("小さいほど追従が速い。0.02〜0.08くらいで調整")]
    private float rotationSmoothTime = 0.03f;

    // [SerializeField, Tooltip("回転方向が逆なら -1 にする")]
    private float yawSign = 1f;

    // [SerializeField, Tooltip("回転量の倍率。基本は1")]
    private float yawScale = 1f;

    private float startSensorYaw;
    private float startUnityYaw;

    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;

    void Awake()
    {
        if (submarine_speed < 0) submarine_speed = 1;
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
        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        rigidbody.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
        rigidbody.linearVelocity = transform.forward * speed;
    }
}