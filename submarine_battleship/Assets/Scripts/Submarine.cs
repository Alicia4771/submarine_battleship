using UnityEngine;

public class Submarine : Ship
{
    [SerializeField] private float submarine_speed = 1f;
    
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
