using UnityEngine;

public class Submarine : Ship
{
    [SerializeField] private float submarine_speed;
    
    void Awake()
    {
        if (submarine_speed < 0) submarine_speed = 1;
    }

    protected override void Start()
    {
        base.Start();
        this.max_speed = DataManager.GetSubmarineMaxSpeed();
        speed = submarine_speed;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        rigidbody.linearVelocity = transform.forward * speed;
    }

}
